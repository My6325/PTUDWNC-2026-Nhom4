namespace CulinaryBlog.Application.Contracts;

public record GoogleUserInfo(string Subject, string Email, bool EmailVerified, string? Name, string? PictureUrl);

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct);
}
