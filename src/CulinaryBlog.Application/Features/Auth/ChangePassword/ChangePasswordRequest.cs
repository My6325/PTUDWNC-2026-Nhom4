namespace CulinaryBlog.Application.Features.Auth.ChangePassword;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
);
