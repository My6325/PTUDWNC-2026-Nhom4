using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Update;

public sealed record UpdateRecipeCommand(Guid Id, UpdateRecipeRequest Request) : IRequest<RecipeDto>;

public sealed class UpdateRecipeCommandHandler(
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<UpdateRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await unitOfWork.Recipes.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công thức.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        var request = command.Request;
        var requestedRowVersion = RecipeMappings.ToRowVersion(request.RowVersion);
        if (requestedRowVersion != recipe.RowVersion)
        {
            throw new RecipeConcurrencyConflictException();
        }

        if (request.CategoryId.HasValue &&
            !await unitOfWork.Categories.ExistsAsync(request.CategoryId.Value, cancellationToken))
        {
            throw new UnprocessableEntityException("RECIPE_CATEGORY_INVALID", "Category không hợp lệ.");
        }

        var oldSlug = recipe.Slug;
        var newSlug = SlugHelper.GenerateSlug(request.Title);
        if (await unitOfWork.Recipes.SlugExistsAsync(newSlug, recipe.Id, cancellationToken))
        {
            throw new ConflictException("RECIPE_SLUG_CONFLICT", "Slug công thức đã tồn tại.");
        }

        recipe.Update(
            request.Title,
            newSlug,
            request.Description,
            request.Instructions,
            request.CategoryId,
            request.PrepTimeMinutes,
            request.CookTimeMinutes,
            request.Servings,
            request.Difficulty);

        if (request.Nutrition is not null)
        {
            recipe.SetNutrition(new RecipeNutrition
            {
                Calories = request.Nutrition.Calories,
                Protein = request.Nutrition.Protein,
                Carbohydrates = request.Nutrition.Carbohydrates,
                Fat = request.Nutrition.Fat,
                Fiber = request.Nutrition.Fiber,
                Sodium = request.Nutrition.Sodium
            });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateListAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(oldSlug, cancellationToken);
        if (!string.Equals(oldSlug, newSlug, StringComparison.Ordinal))
        {
            await cacheInvalidator.InvalidateDetailAsync(newSlug, cancellationToken);
        }

        return recipe.ToDto();
    }
}
