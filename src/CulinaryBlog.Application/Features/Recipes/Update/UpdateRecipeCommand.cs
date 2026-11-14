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
        var recipe = await unitOfWork.Recipes.GetByIdWithDetailsAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công thức.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);
        CulinaryBlog.Application.Features.Recipes.Management.RecipeEditing.EnsureDraft(recipe);

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

        var instructions = !string.IsNullOrWhiteSpace(request.Instructions)
            ? request.Instructions
            : request.Steps is { Count: > 0 }
                ? string.Join("\n", request.Steps.Select((s, i) =>
                    s.Title.Trim().StartsWith("Bước", StringComparison.OrdinalIgnoreCase)
                        ? s.Title.Trim()
                        : $"Bước {i + 1}: {s.Title.Trim()}"))
                : recipe.Instructions;

        recipe.Update(
            request.Title,
            newSlug,
            request.Description,
            instructions,
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

        if (request.Steps is not null)
        {
            var activeSteps = recipe.Steps.Where(s => !s.IsDeleted).ToList();
            var stepIdsToKeep = new HashSet<Guid>();

            for (var index = 0; index < request.Steps.Count; index++)
            {
                var stepReq = request.Steps[index];
                if (stepReq.Id.HasValue && activeSteps.FirstOrDefault(s => s.Id == stepReq.Id.Value) is { } existingStep)
                {
                    existingStep.Update(stepReq.Title, stepReq.Description, stepReq.TimerMinutes, stepReq.ImageUrl);
                    existingStep.StepNumber = index + 1;
                    stepIdsToKeep.Add(existingStep.Id);
                }
                else
                {
                    var newStep = RecipeStep.Create(
                        recipe.Id,
                        index + 1,
                        stepReq.Title,
                        stepReq.Description,
                        stepReq.TimerMinutes,
                        stepReq.ImageUrl);
                    recipe.AddStep(newStep);
                }
            }

            foreach (var oldStep in activeSteps.Where(s => !stepIdsToKeep.Contains(s.Id)))
            {
                recipe.RemoveStep(oldStep.Id);
            }

            recipe.RenumberSteps();
        }

        if (request.Ingredients is not null)
        {
            var activeIngredients = recipe.Ingredients.Where(i => !i.IsDeleted).ToList();
            var ingredientIdsToKeep = new HashSet<Guid>();

            for (var index = 0; index < request.Ingredients.Count; index++)
            {
                var ingReq = request.Ingredients[index];
                if (ingReq.Id.HasValue && activeIngredients.FirstOrDefault(i => i.Id == ingReq.Id.Value) is { } existingIng)
                {
                    existingIng.Update(ingReq.Name, ingReq.Quantity, ingReq.Unit, ingReq.Notes);
                    existingIng.OrderIndex = index + 1;
                    ingredientIdsToKeep.Add(existingIng.Id);
                }
                else
                {
                    var newIng = RecipeIngredient.Create(
                        recipe.Id,
                        index + 1,
                        ingReq.Name,
                        ingReq.Quantity,
                        ingReq.Unit,
                        ingReq.Notes);
                    recipe.AddIngredient(newIng);
                }
            }

            foreach (var oldIng in activeIngredients.Where(i => !ingredientIdsToKeep.Contains(i.Id)))
            {
                recipe.RemoveIngredient(oldIng.Id);
            }

            recipe.RenumberIngredients();
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
