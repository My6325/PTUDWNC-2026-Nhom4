using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Delete;

public sealed class DeleteRecipeValidator : AbstractValidator<DeleteRecipeCommand>
{
    public DeleteRecipeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id công thức không được để trống.");
    }
}
