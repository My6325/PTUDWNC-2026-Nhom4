using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Images.DeleteImage;

public sealed class DeleteRecipeImageValidator : AbstractValidator<DeleteRecipeImageCommand>
{
    public DeleteRecipeImageValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty()
            .WithMessage("RecipeId không được để trống.");

        RuleFor(x => x.ImageId)
            .NotEmpty()
            .WithMessage("ImageId không được để trống.");
    }
}
