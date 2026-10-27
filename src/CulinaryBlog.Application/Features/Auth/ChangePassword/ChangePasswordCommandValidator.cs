using FluentValidation;

namespace CulinaryBlog.Application.Features.Auth.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.Request.CurrentPassword)
            .NotEmpty()
            .WithMessage("Vui lòng nhập mật khẩu hiện tại.");

        RuleFor(x => x.Request.NewPassword)
            .NotEmpty()
            .WithMessage("Vui lòng nhập mật khẩu mới.")
            .MinimumLength(8)
            .WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage("Mật khẩu mới phải bao gồm ít nhất một chữ hoa, một chữ thường và một chữ số.")
            .Must((cmd, newPass) => newPass != cmd.Request.CurrentPassword)
            .WithMessage("Mật khẩu mới không được trùng với mật khẩu hiện tại.");

        RuleFor(x => x.Request.ConfirmNewPassword)
            .NotEmpty()
            .WithMessage("Vui lòng xác nhận lại mật khẩu mới.")
            .Equal(x => x.Request.NewPassword)
            .WithMessage("Xác nhận mật khẩu mới không trùng khớp.");
    }
}
