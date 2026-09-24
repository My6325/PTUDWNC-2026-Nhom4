using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Update;

public sealed class UpdateRecipeValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Request.Title)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(200);
        RuleFor(command => command.Request.Description).NotEmpty();
        RuleFor(command => command.Request.PrepTimeMinutes).GreaterThan(0);
        RuleFor(command => command.Request.CookTimeMinutes).GreaterThan(0);
        RuleFor(command => command.Request.Servings).GreaterThan(0);
        RuleFor(command => command.Request.Difficulty).IsInEnum();
        RuleFor(command => command.Request.RowVersion)
            .NotNull()
            .Must(rowVersion => rowVersion.Length == sizeof(uint))
            .WithMessage("RowVersion phải là chuỗi Base64 biểu diễn đúng 4 byte.");
    }
}
