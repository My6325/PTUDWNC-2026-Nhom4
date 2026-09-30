namespace CulinaryBlog.Application.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    string UserId,
    string Email,
    string DisplayName,
    IList<string> Roles);
