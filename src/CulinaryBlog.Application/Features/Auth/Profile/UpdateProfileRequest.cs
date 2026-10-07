namespace CulinaryBlog.Application.Features.Auth.Profile;

public record UpdateProfileRequest(
    string? DisplayName,
    string? AvatarUrl,
    string? Bio
);
