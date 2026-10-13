using CulinaryBlog.API.Authorization;
using CulinaryBlog.API.Caching;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.API.ExceptionHandling;
using CulinaryBlog.API.OpenApi;
using CulinaryBlog.Application;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.HealthChecks;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seeders;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Hangfire;

// 1. Tự động tìm và nạp biến môi trường từ file .env ở thư mục gốc dự án
var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, ".env")))
{
    currentDir = currentDir.Parent;
}
if (currentDir != null)
{
    var envFile = Path.Combine(currentDir.FullName, ".env");
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
        var parts = trimmed.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var val = parts[1].Trim().Trim('"', '\'');
            if (val.StartsWith("SUPABASE_CONNECTION_STRING=", StringComparison.OrdinalIgnoreCase))
            {
                val = val.Substring("SUPABASE_CONNECTION_STRING=".Length).Trim().Trim('"', '\'');
            }
            Environment.SetEnvironmentVariable(key, val);
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// 2. Đăng ký các dịch vụ tầng Application (MediatR, FluentValidation, Mapster)
builder.Services.AddApplication();

// 3. Đăng ký In-Memory Cache cho Danh mục ẩm thực (TTL 60 phút)
builder.Services.AddMemoryCache();

// 4. Đăng ký các dịch vụ tầng Infrastructure (DbContext, Identity Core, JWT, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// 5. Đăng ký Exception Handling & Problem Details
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 5.1. Cấu hình CORS với allowed origins từ appsettings hoặc biến môi trường FRONTEND_ORIGINS
var corsAllowedOrigins = builder.Configuration["FRONTEND_ORIGINS"]
    ?? builder.Configuration["Cors:AllowedOrigins"];

var allowedOrigins = !string.IsNullOrWhiteSpace(corsAllowedOrigins)
    ? corsAllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : builder.Environment.IsDevelopment()
        ? ["http://localhost:3000"]
        : Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "Accept", "X-Requested-With", "X-Correlation-ID", "traceparent")
                  .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

// 5.2. Cấu hình Rate Limiting bảo vệ các endpoint nhạy cảm (auth-sensitive)
var authRateLimitPermit = builder.Configuration.GetValue<int?>("RateLimiting:AuthSensitive:PermitLimit") ?? 10;
var authRateLimitWindowSec = builder.Configuration.GetValue<int?>("RateLimiting:AuthSensitive:WindowSeconds") ?? 60;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Quá nhiều yêu cầu",
            Detail = "Bạn đã gửi quá nhiều yêu cầu xác thực trong thời gian ngắn. Vui lòng thử lại sau.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["code"] = "TOO_MANY_REQUESTS";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
            problem.Extensions["retryAfterSeconds"] = (int)retryAfter.TotalSeconds;
        }

        await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: token);
    };

    options.AddPolicy("auth-sensitive", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimitPermit,
                Window = TimeSpan.FromSeconds(authRateLimitWindowSec),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

// 6. Đăng ký Authorization & Cache Services cho Recipes
builder.Services.AddSingleton<IAuthorizationHandler, RecipeAuthorizationHandler>();
builder.Services.AddScoped<IRecipeAuthorizationService, RecipeAuthorizationService>();
builder.Services.AddScoped<IRecipeCacheInvalidator, RecipeCacheInvalidator>();

// 7. Đăng ký Output Cache cho Recipes
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("RecipesCache", policy => policy
        .Expire(TimeSpan.FromMinutes(15))
        .SetVaryByQuery("*")
        .Tag("recipes"));
    options.AddPolicy("RecipeDetail", policy => policy
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByRouteValue("slug"));
});

// 8. Đăng ký tài liệu OpenAPI 3.x native của .NET 10 kèm cấu hình Bearer Authentication cho Scalar UI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// 8.1. Đăng ký API Versioning với Asp.Versioning.Http & ApiExplorer
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"),
        new QueryStringApiVersionReader("api-version")
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// 9. Đăng ký Health Checks cho hệ thống (Liveness & Supabase Readiness)
builder.Services.AddHealthChecks()
    .AddCheck<SupabaseDatabaseHealthCheck>("supabase-postgres", tags: ["ready"]);

var app = builder.Build();

// 10. Cấu hình Middleware pipeline theo thứ tự chuẩn
app.UseExceptionHandler();
app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

// 10.1. Cấu hình Hangfire Dashboard (bảo vệ bằng HangfireAdminAuthorizationFilter, không để public)
var hangfireEnabled = bool.TryParse(builder.Configuration["Hangfire:Enabled"] ?? Environment.GetEnvironmentVariable("HANGFIRE_ENABLED"), out var hf) && hf;
if (hangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new CulinaryBlog.Infrastructure.Jobs.HangfireAdminAuthorizationFilter()]
    });
}

// 11. Cấu hình OpenAPI và Scalar UI tương tác trong môi trường Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Culinary Blog API Documentation v1")
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// 12. Tự động áp dụng migration trước khi seed để bảo đảm schema đã tồn tại.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}

// 13. Tự động kiểm tra và nạp dữ liệu mẫu (Seeder) khi khởi động server
await CulinaryBlogSeeder.SeedAsync(app.Services);

// 14. Khởi tạo ApiVersionSet cho API v1.0
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

// 15. Đăng ký các endpoints nghiệp vụ của cả nhóm gắn với Version Set v1
app.MapCategoryEndpoints(apiVersionSet);
app.MapRecipeEndpoints(apiVersionSet);
app.MapAuthEndpoints(apiVersionSet);
app.MapHealthCheckEndpoints();

// 16. Chuyển hướng trang chủ sang giao diện tài liệu Scalar UI
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();
