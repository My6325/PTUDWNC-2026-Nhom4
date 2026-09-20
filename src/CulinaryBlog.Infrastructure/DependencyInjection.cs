using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Settings;
using CulinaryBlog.Infrastructure.Services;
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
    /// Hiện tại bao gồm: cấu hình JWT và dịch vụ JwtService.
    /// Các dịch vụ khác (DbContext, Hangfire...) sẽ được bổ sung ở các bước tiếp theo.
    /// </summary>
    /// <param name="services">Service collection của ứng dụng</param>
    /// <param name="configuration">Cấu hình ứng dụng (appsettings.json / biến môi trường)</param>
    /// <returns>Service collection đã được bổ sung dịch vụ Infrastructure</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind section "Jwt" từ appsettings.json vào JwtSettings POCO
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        // Đăng ký JwtService với lifetime Scoped (mỗi HTTP request một instance)
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }
}
