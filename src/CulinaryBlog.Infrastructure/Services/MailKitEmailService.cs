using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CulinaryBlog.Infrastructure.Services;

/// <summary>
/// Cài đặt dịch vụ gửi email sử dụng thư viện MailKit và giao thức SMTP chuẩn.
/// </summary>
public class MailKitEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IOptions<SmtpSettings> settings, ILogger<MailKitEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string displayName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("SendWelcomeEmailAsync: Địa chỉ toEmail rỗng, bỏ qua gửi email.");
            return;
        }

        // Kiểm tra xem máy chủ SMTP thực tế đã được cấu hình hay chưa
        var isSmtpConfigured = !string.IsNullOrWhiteSpace(_settings.Host) &&
                               _settings.Host != "localhost" &&
                               _settings.Host != "smtp.example.com";

        if (!isSmtpConfigured)
        {
            _logger.LogInformation(
                "MailKitEmailService: Máy chủ SMTP thực tế chưa được cấu hình (Host: {Host}). Giả lập gửi email chào mừng thành công đến '{ToEmail}' cho '{DisplayName}'.",
                _settings.Host,
                toEmail,
                displayName);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(displayName, toEmail));
            message.Subject = "Chào mừng bạn đến với Culinary Blog!";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 8px; background-color: #ffffff;"">
    <div style=""text-align: center; margin-bottom: 24px;"">
        <h1 style=""color: #7c3aed; margin: 0; font-size: 28px;"">🍳 Culinary Blog</h1>
        <p style=""color: #64748b; font-size: 14px; margin-top: 4px;"">Khám phá & Chia sẻ Tinh hoa Ẩm thực</p>
    </div>
    <div style=""color: #334155; line-height: 1.6; font-size: 16px;"">
        <p>Xin chào <strong>{displayName}</strong>,</p>
        <p>Cảm ơn bạn đã đăng ký tài khoản tại <strong>Culinary Blog</strong>! Chúng tôi rất vui mừng được chào đón bạn gia nhập cộng đồng những người đam mê ẩm thực.</p>
        <p>Với tài khoản mới của mình, bạn có thể:</p>
        <ul style=""padding-left: 20px;"">
            <li>Khám phá hàng trăm công thức nấu ăn phong phú, dễ làm.</li>
            <li>Sáng tạo và đăng tải các công thức nấu ăn độc đáo của riêng bạn.</li>
            <li>Lưu lại các công thức yêu thích và chia sẻ cùng bạn bè.</li>
        </ul>
        <div style=""text-align: center; margin: 32px 0;"">
            <a href=""http://localhost:3000"" style=""background-color: #7c3aed; color: #ffffff; padding: 12px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; display: inline-block;"">Bắt đầu khám phá</a>
        </div>
        <p style=""font-size: 14px; color: #64748b;"">Nếu bạn có bất kỳ câu hỏi nào, xin vui lòng phản hồi email này để được đội ngũ hỗ trợ.</p>
    </div>
    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;"" />
    <div style=""text-align: center; font-size: 12px; color: #94a3b8;"">
        <p>© 2026 Culinary Blog. Tất cả các quyền được bảo lưu.</p>
    </div>
</div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            var socketOptions = _settings.EnableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.UserName) && !string.IsNullOrWhiteSpace(_settings.Password))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("MailKitEmailService: Đã gửi email chào mừng thành công tới '{ToEmail}'.", toEmail);
        }
        catch (Exception ex)
        {
            // Logging an toàn: không in password/secrets
            _logger.LogError(ex, "MailKitEmailService: Thất bại khi gửi email chào mừng tới '{ToEmail}'.", toEmail);
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string displayName,
        string resetToken,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("SendPasswordResetEmailAsync: Địa chỉ toEmail rỗng, bỏ qua gửi email.");
            return;
        }

        var isSmtpConfigured = !string.IsNullOrWhiteSpace(_settings.Host) &&
                               _settings.Host != "localhost" &&
                               _settings.Host != "smtp.example.com";

        if (!isSmtpConfigured)
        {
            _logger.LogInformation(
                "MailKitEmailService: Máy chủ SMTP thực tế chưa được cấu hình (Host: {Host}). Giả lập gửi email đặt lại mật khẩu thành công đến '{ToEmail}'. Link: {ResetUrl}",
                _settings.Host,
                toEmail,
                resetUrl);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(displayName, toEmail));
            message.Subject = "Yêu cầu đặt lại mật khẩu — Culinary Blog";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<div style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 8px; background-color: #ffffff;"">
    <div style=""text-align: center; margin-bottom: 24px;"">
        <h1 style=""color: #7c3aed; margin: 0; font-size: 28px;"">🍳 Culinary Blog</h1>
        <p style=""color: #64748b; font-size: 14px; margin-top: 4px;"">Khám phá & Chia sẻ Tinh hoa Ẩm thực</p>
    </div>
    <div style=""color: #334155; line-height: 1.6; font-size: 16px;"">
        <p>Xin chào <strong>{displayName}</strong>,</p>
        <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với địa chỉ email <strong>{toEmail}</strong> trên hệ thống Culinary Blog.</p>
        <p>Để hoàn tất việc thiết lập mật khẩu mới, bạn vui lòng nhấn vào nút bên dưới:</p>
        <div style=""text-align: center; margin: 32px 0;"">
            <a href=""{resetUrl}"" style=""background-color: #7c3aed; color: #ffffff; padding: 12px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; display: inline-block;"">Đặt lại mật khẩu</a>
        </div>
        <p style=""font-size: 14px; color: #64748b;"">Nếu nút trên không hoạt động, bạn có thể sao chép liên kết sau và dán vào trình duyệt:</p>
        <p style=""font-size: 13px; word-break: break-all; color: #7c3aed;"">{resetUrl}</p>
        <div style=""background-color: #f8fafc; border-left: 4px solid #7c3aed; padding: 12px; margin: 20px 0; border-radius: 4px;"">
            <p style=""margin: 0; font-size: 13px; color: #475569;"">
                <strong>Lưu ý bảo mật:</strong> Liên kết đặt lại mật khẩu này chỉ có hiệu lực trong vòng 24 giờ. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email và tài khoản của bạn vẫn an toàn tuyệt đối.
            </p>
        </div>
    </div>
    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;"" />
    <div style=""text-align: center; font-size: 12px; color: #94a3b8;"">
        <p>© 2026 Culinary Blog. Tất cả các quyền được bảo lưu.</p>
    </div>
</div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            var socketOptions = _settings.EnableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.UserName) && !string.IsNullOrWhiteSpace(_settings.Password))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("MailKitEmailService: Đã gửi email đặt lại mật khẩu thành công tới '{ToEmail}'.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MailKitEmailService: Thất bại khi gửi email đặt lại mật khẩu tới '{ToEmail}'.", toEmail);
            throw;
        }
    }
}
