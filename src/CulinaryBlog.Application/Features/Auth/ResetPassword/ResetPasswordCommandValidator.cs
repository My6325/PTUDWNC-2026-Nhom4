using FluentValidation;

namespace CulinaryBlog.Application.Features.Auth.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty()
            .WithMessage("Vui lòng nhập địa chỉ email.")
            .EmailAddress()
            .WithMessage("Địa chỉ email không hợp lệ.");

        RuleFor(x => x.Request.Token)
            .NotEmpty()
            .WithMessage("Mã xác thực đặt lại mật khẩu không được để trống.");

        RuleFor(x => x.Request.NewPassword)
            .NotEmpty()
            .WithMessage("Vui lòng nhập mật khẩu mới.")
            .MinimumLength(8)
            .WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage("Mật khẩu mới phải bao gồm ít nhất một chữ hoa, một chữ thường và một chữ số.");

        RuleFor(x => x.Request.ConfirmNewPassword)
            .NotEmpty()
            .WithMessage("Vui lòng xác nhận lại mật khẩu mới.")
            .Equal(x => x.Request.NewPassword)
            .WithMessage("Xác nhận mật khẩu mới không trùng khớp.");
    }
}
