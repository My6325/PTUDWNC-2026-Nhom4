using CulinaryBlog.Application.Behaviors;
using FluentValidation;
using Mapster;
using MediatR;
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
        var assembly = typeof(DependencyInjection).Assembly;

        // 1. Đăng ký MediatR và Pipeline Behaviors
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // 2. Đăng ký FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // 3. Tự động quét và đăng ký cấu hình ánh xạ DTO Mapster
        TypeAdapterConfig.GlobalSettings.Scan(assembly);

        return services;
    }
}
