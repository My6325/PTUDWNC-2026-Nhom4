namespace CulinaryBlog.Domain.Exceptions;

/// <summary>
/// Refresh token hết hạn, đã bị thu hồi hoặc sai chữ ký SHA-256.
/// </summary>
public sealed class InvalidRefreshTokenException(string message = "Refresh token không hợp lệ hoặc đã hết hạn.", string code = "AUTH_REFRESH_TOKEN_INVALID")
    : DomainException(message, code);
