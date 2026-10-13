using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.API.Security;

public static class RequestSecurityExtensions
{
    public static IServiceCollection AddFrontendRequestSecurity(this IServiceCollection services)
    {
        // 10. Đăng ký CORS cho phép Frontend (Next.js port 3000) truy cập API
        services.AddCors(options =>
        {
            options.AddPolicy("FrontendPolicy", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:3000",
                        "https://localhost:3000"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // 11. Đăng ký Rate Limiting bảo vệ hệ thống chống DDoS / Brute-force
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status429TooManyRequests,
                    title = "Too Many Requests",
                    detail = "Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng thử lại sau 1 phút.",
                    instance = context.HttpContext.Request.Path
                }, cancellationToken: token);
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
        });

        return services;
    }
}
