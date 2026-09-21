using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Settings;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
            });
        });

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

        // 3. Bind section "Jwt" từ appsettings.json vào JwtSettings POCO
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        // 4. Đăng ký JwtService với lifetime Scoped
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }
}
