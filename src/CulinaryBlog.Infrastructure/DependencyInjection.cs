using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Settings;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Repositories;
using CulinaryBlog.Infrastructure.Services;
using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;

namespace CulinaryBlog.Infrastructure;

/// <summary>
/// Điểm đăng ký Dependency Injection tập trung cho toàn bộ tầng Infrastructure.
/// Được gọi từ Program.cs ở tầng API thông qua extension method <see cref="AddInfrastructure"/>.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký các dịch vụ Infrastructure vào DI container.
    /// Bao gồm: Database DbContext (Supabase PostgreSQL), Identity Core, và dịch vụ JWT.
    /// </summary>
    /// <param name="services">Service collection của ứng dụng</param>
    /// <param name="configuration">Cấu hình ứng dụng (appsettings.json / biến môi trường)</param>
    /// <returns>Service collection đã được bổ sung dịch vụ Infrastructure</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Cấu hình kết nối Supabase Cloud PostgreSQL qua EF Core 10
        var connectionString = SupabaseConnectionStringResolver.Resolve(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // 2. Cấu hình ASP.NET Core Identity & Data Protection
        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // 3. Bind JWT từ appsettings hoặc các biến trong file .env của dự án.
        var jwtSettings = new JwtSettings
        {
            Secret = configuration["Jwt:Secret"]
                ?? Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? throw new InvalidOperationException("JWT_SECRET chưa được cấu hình."),
            Issuer = configuration["Jwt:Issuer"]
                ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? "CulinaryBlogAPI",
            Audience = configuration["Jwt:Audience"]
                ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? "CulinaryBlogClient",
            AccessTokenExpirationMinutes = configuration.GetValue<int?>("Jwt:AccessTokenExpirationMinutes") ?? 15,
            RefreshTokenExpirationDays = configuration.GetValue<int?>("Jwt:RefreshTokenExpirationDays") ?? 7
        };

        services.Configure<JwtSettings>(options =>
        {
            options.Secret = jwtSettings.Secret;
            options.Issuer = jwtSettings.Issuer;
            options.Audience = jwtSettings.Audience;
            options.AccessTokenExpirationMinutes = jwtSettings.AccessTokenExpirationMinutes;
            options.RefreshTokenExpirationDays = jwtSettings.RefreshTokenExpirationDays;
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        // 4. Đăng ký JwtService với lifetime Scoped
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IClientContext, ClientContext>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        // 5. Đăng ký repository nghiệp vụ
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 6. Đăng ký IApplicationDbContext phục vụ tầng Application
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // 7. Đăng ký dịch vụ lưu trữ Supabase Storage
        services.AddHttpClient<ISupabaseStorageService, SupabaseStorageService>();

        // 8. Đăng ký Background Job xử lý ảnh (FR-JOB-002)
        services.AddHttpClient<ImageResizeJob>();
        services.AddScoped<IImageResizeJob>(sp => sp.GetRequiredService<ImageResizeJob>());

        // 9. Cấu hình SMTP & Email Service (FR-JOB-001)
        var smtpSettings = new SmtpSettings
        {
            Host = configuration["Smtp:Host"] ?? Environment.GetEnvironmentVariable("SMTP_HOST") ?? "localhost",
            Port = int.TryParse(configuration["Smtp:Port"] ?? Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587,
            EnableSsl = bool.TryParse(configuration["Smtp:EnableSsl"] ?? Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL"), out var ssl) ? ssl : true,
            UserName = configuration["Smtp:UserName"] ?? Environment.GetEnvironmentVariable("SMTP_USERNAME"),
            Password = configuration["Smtp:Password"] ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
            FromEmail = configuration["Smtp:FromEmail"] ?? Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? "noreply@culinaryblog.local",
            FromName = configuration["Smtp:FromName"] ?? Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "Culinary Blog"
        };

        services.Configure<SmtpSettings>(options =>
        {
            options.Host = smtpSettings.Host;
            options.Port = smtpSettings.Port;
            options.EnableSsl = smtpSettings.EnableSsl;
            options.UserName = smtpSettings.UserName;
            options.Password = smtpSettings.Password;
            options.FromEmail = smtpSettings.FromEmail;
            options.FromName = smtpSettings.FromName;
        });

        services.AddScoped<IEmailService, MailKitEmailService>();
        services.AddScoped<IBackgroundJobService, Jobs.BackgroundJobService>();
        services.AddScoped<Jobs.WelcomeEmailJob>();
        services.AddScoped<Jobs.GoogleSearchConsoleSitemapSubmitter>();

        // 10. Cấu hình Hangfire Storage (Chỉ kích hoạt khi HANGFIRE_ENABLED=true và có connection string riêng biệt để bảo vệ Supabase Cloud)
        var hangfireEnabled = bool.TryParse(configuration["Hangfire:Enabled"] ?? Environment.GetEnvironmentVariable("HANGFIRE_ENABLED"), out var hf) && hf;
        var hangfireConnStr = configuration["Hangfire:ConnectionString"] ?? Environment.GetEnvironmentVariable("HANGFIRE_CONNECTION_STRING");
        if (hangfireEnabled && !string.IsNullOrWhiteSpace(hangfireConnStr))
        {
            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConnStr));
            });
            services.AddHangfireServer();
        }

        return services;
    }
}
