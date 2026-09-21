namespace CulinaryBlog.Domain.Settings;

/// <summary>
/// Cấu hình JWT (JSON Web Token) dùng để bind từ appsettings.json / biến môi trường
/// thông qua <c>IOptions&lt;JwtSettings&gt;</c> ở tầng Infrastructure / API.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Khóa bí mật (Secret Key) dùng để ký và xác minh chữ ký số của JWT.
    /// Khuyến nghị tối thiểu 256-bit (32 ký tự) cho thuật toán HMAC-SHA256.
    /// </summary>
    public string Secret { get; set; } = null!;

    /// <summary>
    /// Nhà phát hành token (Issuer) — thường là URL hoặc tên của ứng dụng backend.
    /// </summary>
    public string Issuer { get; set; } = null!;

    /// <summary>
    /// Đối tượng nhận token (Audience) — thường là URL hoặc tên của ứng dụng frontend.
    /// </summary>
    public string Audience { get; set; } = null!;

    /// <summary>
    /// Thời gian sống (phút) của Access Token. Mặc định 15 phút.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Thời gian sống (ngày) của Refresh Token. Mặc định 7 ngày.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
