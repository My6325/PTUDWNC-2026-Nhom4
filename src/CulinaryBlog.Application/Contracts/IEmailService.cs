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

    /// <summary>
    /// Gửi email hướng dẫn và liên kết đặt lại mật khẩu cho người dùng.
    /// </summary>
    /// <param name="toEmail">Địa chỉ email người nhận.</param>
    /// <param name="displayName">Tên hiển thị của người dùng.</param>
    /// <param name="resetToken">Mã token đặt lại mật khẩu an toàn.</param>
    /// <param name="resetUrl">Đường dẫn đặt lại mật khẩu trực tiếp trên giao diện Frontend.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetEmailAsync(string toEmail, string displayName, string resetToken, string resetUrl, CancellationToken cancellationToken = default);
}
