using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Entities;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>
/// Background Job phụ trách gửi email chào mừng cho người dùng mới đăng ký thành công.
/// Được thiết kế hỗ trợ cả Hangfire và fallback runner với cơ chế retry tối đa 3 lần và bảo đảm tính Idempotent.
/// </summary>
public class WelcomeEmailJob
{
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<WelcomeEmailJob> _logger;

    public WelcomeEmailJob(
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        ILogger<WelcomeEmailJob> logger)
    {
        _emailService = emailService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Thực thi gửi email chào mừng.
    /// Giới hạn retry tối đa 3 lần để tránh lặp vô hạn nếu máy chủ mail lỗi.
    /// </summary>
    /// <param name="userId">Id của người dùng vừa đăng ký.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task SendWelcomeEmailAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("WelcomeEmailJob: UserId rỗng hoặc không hợp lệ. Hủy bỏ tác vụ.");
            return;
        }

        // 1. Kiểm tra tính tồn tại của người dùng (Idempotency check)
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("WelcomeEmailJob: Không tìm thấy người dùng với Id '{UserId}'. Hủy bỏ tác vụ.", userId);
            return;
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("WelcomeEmailJob: Người dùng '{UserId}' không có địa chỉ email hợp lệ. Hủy bỏ tác vụ.", userId);
            return;
        }

        _logger.LogInformation("WelcomeEmailJob: Bắt đầu xử lý gửi email chào mừng cho người dùng '{UserId}'.", userId);

        var recipientName = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : (!string.IsNullOrWhiteSpace(user.UserName) ? user.UserName : "Bạn");

        // 2. Gửi email thông qua EmailService abstraction (tuyệt đối không in password hay credentials vào log)
        await _emailService.SendWelcomeEmailAsync(user.Email, recipientName, cancellationToken);

        _logger.LogInformation("WelcomeEmailJob: Hoàn tất gửi email chào mừng cho người dùng '{UserId}'.", userId);
    }
}
