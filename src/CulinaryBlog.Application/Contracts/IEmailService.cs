namespace CulinaryBlog.Application.Contracts;

/// <summary>
/// Abstraction dịch vụ gửi email của hệ thống.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi email chào mừng người dùng mới tham gia Culinary Blog.
    /// </summary>
    /// <param name="toEmail">Địa chỉ email người nhận.</param>
    /// <param name="displayName">Tên hiển thị của người dùng.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendWelcomeEmailAsync(string toEmail, string displayName, CancellationToken cancellationToken = default);
}
