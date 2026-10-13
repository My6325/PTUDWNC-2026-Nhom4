namespace CulinaryBlog.Application.DTOs;

public record UserDto(
    string Id,
    string UserName,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IList<string> Roles,
    DateTime CreatedAt
);
