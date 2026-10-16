using CulinaryBlog.Application.Contracts;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>
/// Dịch vụ điều phối Background Jobs của hệ thống.
/// Hỗ trợ cả cơ chế Hangfire phân tán lẫn Fallback scoped runner khi Hangfire storage chưa kích hoạt (nhằm bảo vệ CSDL Supabase Cloud).
/// </summary>
public class BackgroundJobService : IBackgroundJobService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(IServiceProvider serviceProvider, ILogger<BackgroundJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public string EnqueueWelcomeEmail(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("EnqueueWelcomeEmail: UserId rỗng, bỏ qua enqueue.");
            return string.Empty;
        }

        try
        {
            // 1. Ưu tiên Hangfire nếu IBackgroundJobClient đã được đăng ký và storage khả dụng
            var hangfireClient = _serviceProvider.GetService<IBackgroundJobClient>();
            if (hangfireClient != null)
            {
                var jobId = hangfireClient.Enqueue<WelcomeEmailJob>(job => job.SendWelcomeEmailAsync(userId, CancellationToken.None));
                _logger.LogInformation("EnqueueWelcomeEmail: Đã enqueue thành công qua Hangfire (JobId: '{JobId}') cho user '{UserId}'.", jobId, userId);
                return jobId;
            }

            // 2. Chế độ Fallback: Chạy bất đồng bộ an toàn qua IServiceScopeFactory (không chặn luồng đăng ký)
            _logger.LogInformation("EnqueueWelcomeEmail: Hangfire Storage không kích hoạt (để bảo vệ Supabase DB). Khởi chạy WelcomeEmailJob qua Fallback Runner cho user '{UserId}'.", userId);

            var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var job = scope.ServiceProvider.GetRequiredService<WelcomeEmailJob>();
                    await job.SendWelcomeEmailAsync(userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình thực thi WelcomeEmailJob fallback cho user '{UserId}'.", userId);
                }
            });

            return "fallback-queued";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thất bại khi enqueue email chào mừng cho user '{UserId}'.", userId);
            return "enqueue-failed";
        }
    }
}
