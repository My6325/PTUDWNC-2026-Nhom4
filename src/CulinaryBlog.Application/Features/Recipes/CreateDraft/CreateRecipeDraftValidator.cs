using CulinaryBlog.Application.Features.Recipes.Common;
using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.CreateDraft;

public sealed class CreateRecipeDraftValidator : AbstractValidator<CreateRecipeDraftCommand>
{
    public CreateRecipeDraftValidator()
    {
        RuleFor(command => command.Request.Title)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(200);

        RuleFor(command => command.Request.PrepTimeMinutes).GreaterThan(0);
        RuleFor(command => command.Request.CookTimeMinutes).GreaterThan(0);
        RuleFor(command => command.Request.Servings).GreaterThan(0);
        RuleFor(command => command.Request.Difficulty).IsInEnum();

        RuleForEach(command => command.Request.Steps)
            .SetValidator(new CreateRecipeStepRequestValidator())
            .When(command => command.Request.Steps is not null);

        RuleForEach(command => command.Request.Ingredients)
            .SetValidator(new CreateRecipeIngredientRequestValidator())
            .When(command => command.Request.Ingredients is not null);
    }
}

internal sealed class CreateRecipeStepRequestValidator : AbstractValidator<CreateRecipeStepRequest>
{
    public CreateRecipeStepRequestValidator()
    {
        RuleFor(step => step.Title).NotEmpty().MaximumLength(200);
        RuleFor(step => step.Description).NotEmpty();
        RuleFor(step => step.TimerMinutes).GreaterThan(0).When(step => step.TimerMinutes.HasValue);
    }
}

internal sealed class CreateRecipeIngredientRequestValidator : AbstractValidator<CreateRecipeIngredientRequest>
{
    public CreateRecipeIngredientRequestValidator()
    {
        RuleFor(ingredient => ingredient.Name).NotEmpty().MaximumLength(200);
        RuleFor(ingredient => ingredient.Quantity).GreaterThan(0).When(ingredient => ingredient.Quantity.HasValue);
    }
}
