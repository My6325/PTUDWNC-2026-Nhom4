using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Application;

/// <summary>
/// Điểm đăng ký Dependency Injection tập trung cho toàn bộ tầng Application.
/// Tự động quét và đăng ký MediatR handlers, pipeline behaviors và cấu hình Mapster.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 1. Đăng ký MediatR tự động quét toàn bộ Request/Handler trong Assembly Application
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // 2. Tự động quét và đăng ký cấu hình ánh xạ DTO Mapster
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);

        return services;
    }
}
