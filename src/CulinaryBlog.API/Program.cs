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
using CulinaryBlog.API.Security;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using CulinaryBlog.Infrastructure.Jobs;
using CulinaryBlog.API.BackgroundJobs;

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
var hangfireEnabled = bool.TryParse(builder.Configuration["Hangfire:Enabled"] ?? Environment.GetEnvironmentVariable("HANGFIRE_ENABLED"), out var hf) && hf;
var hangfireConnStr = builder.Configuration["Hangfire:ConnectionString"] ?? Environment.GetEnvironmentVariable("HANGFIRE_CONNECTION_STRING");
if (hangfireEnabled && !string.IsNullOrWhiteSpace(hangfireConnStr))
{
    GlobalConfiguration.Configuration.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnStr));
    builder.Services.AddSingleton<IHostedService, HangfireServerHostedService>();
}
builder.Services.AddScoped<SitemapGenerationJob>();
builder.Services.AddScoped<GoogleSearchConsoleSitemapSubmitter>();
builder.Services.AddHttpClient();

// 5. Đăng ký Exception Handling & Problem Details
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 5.1. Cấu hình CORS với allowed origins từ appsettings hoặc biến môi trường FRONTEND_ORIGINS
var corsAllowedOrigins = builder.Configuration["FRONTEND_ORIGINS"]
    ?? builder.Configuration["Cors:AllowedOrigins"];

var allowedOrigins = !string.IsNullOrWhiteSpace(corsAllowedOrigins)
    ? corsAllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : builder.Environment.IsDevelopment()
        ? ["http://localhost:3000", "https://localhost:3000"]
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

// 5.2. Cấu hình Rate Limiting bảo vệ toàn diện hệ thống và các endpoint nhạy cảm (auth-sensitive)
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
            Detail = "Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng thử lại sau.",
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

    // Giới hạn chung 60 requests / phút theo từng địa chỉ IP của Client
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 2
        });
    });

    // Chính sách bảo vệ chuyên biệt cho Auth Endpoints nhạy cảm
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
        .Expire(TimeSpan.FromMinutes(60))
        .SetVaryByRouteValue("slug"));
});

// 8. Đăng ký tài liệu OpenAPI 3.x native của .NET 10 kèm cấu hình Bearer Authentication cho Scalar UI
builder.Services.AddCulinaryOpenApi();

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

if (hangfireEnabled && !string.IsNullOrWhiteSpace(hangfireConnStr))
{
    GlobalConfiguration.Configuration.UseActivator(new ScopedHangfireJobActivator(app.Services.GetRequiredService<IServiceScopeFactory>()));
    RecurringJob.AddOrUpdate<SitemapGenerationJob>("sitemap-job", job => job.ExecuteAsync(CancellationToken.None), "0 2 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}

// 10. Cấu hình Middleware pipeline theo thứ tự chuẩn
app.UseExceptionHandler();
app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

// 10.1. Cấu hình Hangfire Dashboard (bảo vệ bằng HangfireAdminAuthorizationFilter, không để public)
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

// 12. Migration/seed chỉ chạy khi được bật rõ ràng; WBS yêu cầu điều phối migration tập trung.
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}
else
{
    app.Logger.LogInformation("Automatic database migrations are disabled. Apply migrations through the team's centralized process.");
}

if (builder.Configuration.GetValue<bool>("Database:SeedOnStartup"))
{
    await CulinaryBlogSeeder.SeedAsync(app.Services);
}
else
{
    app.Logger.LogInformation("Automatic sample data seeding is disabled.");
}

// 13. Khởi tạo ApiVersionSet cho API v1.0
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

// 14. Đăng ký các endpoints nghiệp vụ của cả nhóm gắn với Version Set v1
app.MapCategoryEndpoints(apiVersionSet);
app.MapRecipeEndpoints(apiVersionSet);
app.MapAuthEndpoints(apiVersionSet);

// 15. Tuyến sitemap public phục vụ tìm kiếm SEO
app.MapGet("/sitemap.xml", async (IConfiguration configuration, SitemapGenerationJob sitemapJob, CancellationToken ct) =>
{
    if (!SitemapXmlBuilder.IsValidPublicBaseUrl(configuration["PublicSite:BaseUrl"]))
    {
        return Results.Problem("PublicSite:BaseUrl must be configured as an absolute public site URL.", statusCode: 503);
    }

    return Results.Text(await sitemapJob.GenerateXmlAsync(ct), "application/xml; charset=utf-8");
})
    .WithName("GetSitemap")
    .Produces(StatusCodes.Status200OK, contentType: "application/xml")
    .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
app.MapHealthCheckEndpoints();

// 16. Chuyển hướng trang chủ sang giao diện tài liệu Scalar UI
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();
