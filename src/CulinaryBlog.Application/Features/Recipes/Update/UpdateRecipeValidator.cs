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

        RuleForEach(command => command.Request.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Title).NotEmpty().MaximumLength(200);
            step.RuleFor(s => s.Description).NotEmpty();
            step.RuleFor(s => s.TimerMinutes).GreaterThan(0).When(s => s.TimerMinutes.HasValue);
        }).When(command => command.Request.Steps is not null);

        RuleForEach(command => command.Request.Ingredients).ChildRules(ingredient =>
        {
            ingredient.RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
            ingredient.RuleFor(i => i.Quantity).GreaterThan(0).When(i => i.Quantity.HasValue);
        }).When(command => command.Request.Ingredients is not null);
    }
}
