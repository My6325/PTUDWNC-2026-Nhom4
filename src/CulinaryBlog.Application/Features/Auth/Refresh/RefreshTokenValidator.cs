using FluentValidation;

namespace CulinaryBlog.Application.Features.Auth.Refresh;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.Request.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token không được để trống.");
    }
}
