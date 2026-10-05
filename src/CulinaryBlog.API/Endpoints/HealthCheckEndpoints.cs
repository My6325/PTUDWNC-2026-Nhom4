using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.API.Endpoints;

/// <summary>
/// Đăng ký các endpoints Health Checks cho phân hệ Giám sát hệ thống (FR-OBS-001).
/// </summary>
public static class HealthCheckEndpoints
{
    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. GET /health/live (Liveness Probe - Kiểm tra tiến trình ứng dụng)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        })
        .WithName("LivenessProbe")
        .WithSummary("Liveness Probe kiểm tra tiến trình API đang hoạt động")
        .WithDescription("Trả về 200 Healthy nếu ứng dụng đang chạy. Không kiểm tra database.")
        .WithTags("Health & Monitoring");

        // 2. GET /health/ready (Readiness Probe - Kiểm tra kết nối CSDL)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        })
        .WithName("ReadinessProbe")
        .WithSummary("Readiness Probe kiểm tra kết nối CSDL Supabase")
        .WithDescription("Kiểm tra kết nối mạng tới Supabase Cloud PostgreSQL. Trả về 200 Healthy hoặc 503 Unhealthy.")
        .WithTags("Health & Monitoring");

        // 3. GET /health (Báo cáo tổng hợp tình trạng sức khỏe định dạng JSON)
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    totalDuration = $"{report.TotalDuration.TotalMilliseconds:F1}ms",
                    timestamp = DateTime.UtcNow,
                    entries = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        duration = $"{e.Value.Duration.TotalMilliseconds:F1}ms",
                        description = e.Value.Description,
                        exception = e.Value.Exception?.Message
                    })
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        })
        .WithName("OverallHealthReport")
        .WithSummary("Báo cáo tổng hợp sức khỏe toàn hệ thống (JSON)")
        .WithDescription("Báo cáo chi tiết định dạng JSON về tiến trình API, CSDL Supabase và độ trễ phản hồi.")
        .WithTags("Health & Monitoring");

        return app;
    }
}
