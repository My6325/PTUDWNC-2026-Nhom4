using Hangfire.Dashboard;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>
/// Bộ lọc bảo mật cho Hangfire Dashboard: Chỉ cho phép người dùng có vai trò 'Admin' truy cập.
/// Tuyệt đối không để dashboard công khai trên môi trường Production.
/// </summary>
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 1. Phải được xác thực
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // 2. Phải có vai trò Admin
        return httpContext.User.IsInRole("Admin");
    }
}
