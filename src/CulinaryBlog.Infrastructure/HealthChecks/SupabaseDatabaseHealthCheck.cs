using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Infrastructure.HealthChecks;

/// <summary>
/// Lớp kiểm tra sức khỏe kết nối CSDL Supabase PostgreSQL qua EF Core (Readiness Probe).
/// </summary>
public sealed class SupabaseDatabaseHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Kết nối Supabase Cloud PostgreSQL thành công.")
                : HealthCheckResult.Unhealthy("Không thể kết nối đến cơ sở dữ liệu Supabase.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Lỗi kết nối cơ sở dữ liệu Supabase.", ex);
        }
    }
}
