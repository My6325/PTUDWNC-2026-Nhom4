using FluentValidation;

namespace CulinaryBlog.Application.Features.Auth.Register;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, and one number.");

        RuleFor(x => x.Request.DisplayName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Request.UserName)
            .NotEmpty()
            .MaximumLength(50);
    }
}
