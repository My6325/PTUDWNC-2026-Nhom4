using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Ingredients.DeleteIngredient;

public sealed class DeleteIngredientValidator : AbstractValidator<DeleteIngredientCommand>
{
    public DeleteIngredientValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty()
            .WithMessage("RecipeId không được để trống.");

        RuleFor(x => x.IngredientId)
            .NotEmpty()
            .WithMessage("IngredientId không được để trống.");
    }
}
