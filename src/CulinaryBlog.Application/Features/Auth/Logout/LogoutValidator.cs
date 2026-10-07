using FluentValidation;

namespace CulinaryBlog.Application.Features.Auth.Logout;

public class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.Request.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token không được để trống.");
    }
}
