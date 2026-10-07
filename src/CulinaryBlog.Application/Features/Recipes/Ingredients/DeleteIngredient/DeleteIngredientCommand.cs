using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Ingredients.DeleteIngredient;

public sealed record DeleteIngredientCommand(Guid RecipeId, Guid IngredientId) : IRequest;

public sealed class DeleteIngredientCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<DeleteIngredientCommand>
{
    public async Task Handle(DeleteIngredientCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdWithDetailsAsync(command.RecipeId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.RecipeId}'.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        var ingredientExists = recipe.Ingredients.Any(i => i.Id == command.IngredientId && !i.IsDeleted);
        if (!ingredientExists)
        {
            throw new NotFoundException($"Không tìm thấy nguyên liệu với Id '{command.IngredientId}'.");
        }

        recipe.RemoveIngredient(command.IngredientId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}
