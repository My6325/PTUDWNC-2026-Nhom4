using FluentValidation;

namespace CulinaryBlog.Application.Features.Auth.ForgotPassword;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty()
            .WithMessage("Vui lòng nhập địa chỉ email.")
            .EmailAddress()
            .WithMessage("Địa chỉ email không hợp lệ.");
    }
}
