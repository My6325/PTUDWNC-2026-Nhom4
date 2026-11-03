using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Application.Features.Recipes.Update;
using CulinaryBlog.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Recipes.Management;

public sealed record EditorStep(Guid? Id, string Title, string Description, int? TimerMinutes, string? ImageUrl);
public sealed record EditorIngredient(Guid? Id, string Name, decimal? Quantity, string? Unit, string? Notes);
public sealed record SaveManagedRecipeRequest(UpdateRecipeRequest Recipe, IReadOnlyList<EditorStep> Steps,
    IReadOnlyList<EditorIngredient> Ingredients);
public sealed record SaveManagedRecipeCommand(Guid Id, SaveManagedRecipeRequest Request) : IRequest<RecipeDto>;

public sealed class SaveManagedRecipeValidator : AbstractValidator<SaveManagedRecipeCommand>
{
    public SaveManagedRecipeValidator()
    {
        RuleFor(x => x.Request).NotNull();
        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.Recipe).NotNull();
            RuleFor(x => x.Request.Steps).NotNull();
            RuleFor(x => x.Request.Ingredients).NotNull();
            RuleForEach(x => x.Request.Steps).ChildRules(step =>
            {
                step.RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
                step.RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
                step.RuleFor(x => x.TimerMinutes).GreaterThan(0).When(x => x.TimerMinutes.HasValue);
            });
            RuleForEach(x => x.Request.Ingredients).ChildRules(item =>
            {
                item.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
                item.RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
            });
        });
    }
}

public sealed class SaveManagedRecipeHandler(IApplicationDbContext db, IRecipeAuthorizationService authorization,
    IValidator<UpdateRecipeCommand> validator, IRecipeCacheInvalidator cache)
    : IRequestHandler<SaveManagedRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(SaveManagedRecipeCommand command, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(new UpdateRecipeCommand(command.Id, command.Request.Recipe), ct);
        var recipe = await db.Recipes.Include(x => x.Steps).Include(x => x.Ingredients)
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.IsDeleted, ct) ?? throw new NotFoundException("Không tìm thấy công thức.");
        await authorization.EnsureCanUpdateAsync(recipe, ct);
        RecipeEditing.EnsureDraft(recipe);
        var input = command.Request.Recipe;
        RecipeEditing.CheckVersion(recipe, input.RowVersion);
        if (input.CategoryId.HasValue && !await db.Categories.AnyAsync(x => x.Id == input.CategoryId && !x.IsDeleted, ct))
            throw new UnprocessableEntityException("RECIPE_CATEGORY_INVALID", "Danh mục không hợp lệ.");

        var steps = command.Request.Steps;
        var ingredients = command.Request.Ingredients;
        var stepIds = steps.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToArray();
        var ingredientIds = ingredients.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToArray();
        if (stepIds.Distinct().Count() != stepIds.Length || ingredientIds.Distinct().Count() != ingredientIds.Length ||
            stepIds.Any(id => !recipe.Steps.Any(x => x.Id == id && !x.IsDeleted)) ||
            ingredientIds.Any(id => !recipe.Ingredients.Any(x => x.Id == id && !x.IsDeleted)))
            throw new UnprocessableEntityException("RECIPE_CHILD_INVALID", "Bước hoặc nguyên liệu không thuộc công thức này.");

        // Keep the stable slug so saved links and previews continue to work after editing.
        recipe.Update(input.Title, recipe.Slug, input.Description, input.Instructions, input.CategoryId,
            input.PrepTimeMinutes, input.CookTimeMinutes, input.Servings, input.Difficulty);
        if (input.Nutrition is { } n) recipe.SetNutrition(new RecipeNutrition
        { Calories = n.Calories, Protein = n.Protein, Carbohydrates = n.Carbohydrates, Fat = n.Fat, Fiber = n.Fiber, Sodium = n.Sodium });
        foreach (var step in recipe.Steps.Where(x => !x.IsDeleted && !stepIds.Contains(x.Id)).ToArray()) recipe.RemoveStep(step.Id);
        for (var i = 0; i < steps.Count; i++)
        {
            var value = steps[i];
            var step = value.Id.HasValue ? recipe.Steps.Single(x => x.Id == value.Id) :
                RecipeStep.Create(recipe.Id, i + 1, value.Title, value.Description, value.TimerMinutes, value.ImageUrl);
            step.Update(value.Title, value.Description, value.TimerMinutes, value.ImageUrl);
            step.StepNumber = i + 1;
            if (!value.Id.HasValue) { recipe.AddStep(step); db.RecipeSteps.Add(step); }
        }
        foreach (var item in recipe.Ingredients.Where(x => !x.IsDeleted && !ingredientIds.Contains(x.Id)).ToArray()) recipe.RemoveIngredient(item.Id);
        for (var i = 0; i < ingredients.Count; i++)
        {
            var value = ingredients[i];
            var item = value.Id.HasValue ? recipe.Ingredients.Single(x => x.Id == value.Id) :
                RecipeIngredient.Create(recipe.Id, i + 1, value.Name, value.Quantity, value.Unit, value.Notes);
            item.Update(value.Name, value.Quantity, value.Unit, value.Notes);
            item.OrderIndex = i + 1;
            if (!value.Id.HasValue) { recipe.AddIngredient(item); db.RecipeIngredients.Add(item); }
        }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new RecipeConcurrencyConflictException(); }
        await cache.InvalidateDetailAsync(recipe.Slug, ct);
        return recipe.ToDto();
    }
}
