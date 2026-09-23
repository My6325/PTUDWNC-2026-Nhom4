namespace CulinaryBlog.Application.Features.Auth.Register;

public record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string UserName);
