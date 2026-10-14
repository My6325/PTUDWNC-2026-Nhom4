using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Ingredients.UpdateIngredient;

public sealed record UpdateIngredientCommand(Guid RecipeId, Guid IngredientId, UpdateIngredientRequest Request) : IRequest<RecipeIngredientResponseDto>;

public sealed class UpdateIngredientCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<UpdateIngredientCommand, RecipeIngredientResponseDto>
{
    public async Task<RecipeIngredientResponseDto> Handle(UpdateIngredientCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdWithDetailsAsync(command.RecipeId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.RecipeId}'.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        var ingredient = recipe.Ingredients.FirstOrDefault(i => i.Id == command.IngredientId && !i.IsDeleted)
            ?? throw new NotFoundException($"Không tìm thấy nguyên liệu với Id '{command.IngredientId}'.");

        ingredient.Update(
            command.Request.Name,
            command.Request.Quantity,
            command.Request.Unit,
            command.Request.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);

        return new RecipeIngredientResponseDto(
            ingredient.Id,
            ingredient.RecipeId,
            ingredient.Name,
            ingredient.Quantity,
            ingredient.Unit,
            ingredient.Notes,
            ingredient.OrderIndex);
    }
}
