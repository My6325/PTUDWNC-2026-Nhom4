using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.CreateDraft;

public sealed record CreateRecipeDraftCommand(CreateRecipeDraftRequest Request) : IRequest<RecipeDto>;

public sealed class CreateRecipeDraftCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<CreateRecipeDraftCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(
        CreateRecipeDraftCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            throw new UnauthorizedException("Bạn cần đăng nhập để tạo công thức.");
        }

        var request = command.Request;
        if (request.CategoryId.HasValue &&
            !await unitOfWork.Categories.ExistsAsync(request.CategoryId.Value, cancellationToken))
        {
            throw new UnprocessableEntityException("RECIPE_CATEGORY_INVALID", "Category không hợp lệ.");
        }

        var slug = SlugHelper.GenerateSlug(request.Title);
        if (await unitOfWork.Recipes.SlugExistsAsync(slug, null, cancellationToken))
        {
            throw new ConflictException("RECIPE_SLUG_CONFLICT", "Slug công thức đã tồn tại.");
        }

        var instructions = !string.IsNullOrWhiteSpace(request.Instructions)
            ? request.Instructions
            : request.Steps is { Count: > 0 }
                ? string.Join("\n", request.Steps.Select((s, i) =>
                    s.Title.Trim().StartsWith("Bước", StringComparison.OrdinalIgnoreCase)
                        ? s.Title.Trim()
                        : $"Bước {i + 1}: {s.Title.Trim()}"))
                : null;

        var recipe = Recipe.Create(
            request.Title,
            slug,
            request.Description,
            instructions,
            request.CategoryId,
            currentUser.UserId,
            request.PrepTimeMinutes,
            request.CookTimeMinutes,
            request.Servings,
            request.Difficulty);

        recipe.SetNutrition(ToNutrition(request.Nutrition));

        for (var index = 0; index < (request.Steps?.Count ?? 0); index++)
        {
            var step = request.Steps![index];
            recipe.AddStep(RecipeStep.Create(
                recipe.Id,
                index + 1,
                step.Title,
                step.Description,
                step.TimerMinutes,
                step.ImageUrl));
        }

        for (var index = 0; index < (request.Ingredients?.Count ?? 0); index++)
        {
            var ingredient = request.Ingredients![index];
            recipe.AddIngredient(RecipeIngredient.Create(
                recipe.Id,
                index + 1,
                ingredient.Name,
                ingredient.Quantity,
                ingredient.Unit,
                ingredient.Notes));
        }

        await unitOfWork.Recipes.AddAsync(recipe, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateListAsync(cancellationToken);

        return recipe.ToDto();
    }

    private static RecipeNutrition? ToNutrition(RecipeNutritionRequest? request)
    {
        return request is null
            ? null
            : new RecipeNutrition
            {
                Calories = request.Calories,
                Protein = request.Protein,
                Carbohydrates = request.Carbohydrates,
                Fat = request.Fat,
                Fiber = request.Fiber,
                Sodium = request.Sodium
            };
    }
}
