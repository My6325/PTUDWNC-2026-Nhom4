using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Domain.Entities;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Ingredients.AddIngredient;

public sealed record AddIngredientCommand(Guid RecipeId, AddIngredientRequest Request) : IRequest<RecipeIngredientResponseDto>;

public sealed class AddIngredientCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<AddIngredientCommand, RecipeIngredientResponseDto>
{
    public async Task<RecipeIngredientResponseDto> Handle(AddIngredientCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdWithDetailsAsync(command.RecipeId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.RecipeId}'.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        var request = command.Request;
        var nextOrderIndex = recipe.Ingredients.Count(i => !i.IsDeleted) + 1;

        var ingredient = RecipeIngredient.Create(
            recipe.Id,
            nextOrderIndex,
            request.Name,
            request.Quantity,
            request.Unit,
            request.Notes);

        recipe.AddIngredient(ingredient);

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
