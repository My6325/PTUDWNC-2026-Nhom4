namespace CulinaryBlog.Application.Features.Auth.ForgotPassword;

public record ForgotPasswordResponseDto(
    string Message,
    string? ResetToken = null
);
