namespace CulinaryBlog.Domain.Settings;

/// <summary>
/// Cấu hình máy chủ SMTP dùng để gửi email từ backend Culinary Blog
/// thông qua <c>IOptions&lt;SmtpSettings&gt;</c>.
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// Địa chỉ máy chủ SMTP (ví dụ: smtp.gmail.com, smtp.sendgrid.net, mailhog).
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Cổng kết nối SMTP (thường là 587 cho STARTTLS hoặc 465 cho SSL).
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Kích hoạt kết nối bảo mật SSL / TLS. Mặc định true.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Tên người dùng / Email tài khoản xác thực với máy chủ SMTP.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Mật khẩu hoặc App Password của tài khoản SMTP.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Địa chỉ email người gửi hiển thị (From Address).
    /// </summary>
    public string FromEmail { get; set; } = "noreply@culinaryblog.local";

    /// <summary>
    /// Tên đại diện người gửi hiển thị (From Display Name).
    /// </summary>
    public string FromName { get; set; } = "Culinary Blog";
}
