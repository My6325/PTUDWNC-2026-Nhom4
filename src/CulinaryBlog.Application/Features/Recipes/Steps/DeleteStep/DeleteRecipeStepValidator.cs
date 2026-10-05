using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Steps.DeleteStep;

public sealed class DeleteRecipeStepValidator : AbstractValidator<DeleteRecipeStepCommand>
{
    public DeleteRecipeStepValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty()
            .WithMessage("RecipeId không được để trống.");

        RuleFor(x => x.StepId)
            .NotEmpty()
            .WithMessage("StepId không được để trống.");
    }
}
