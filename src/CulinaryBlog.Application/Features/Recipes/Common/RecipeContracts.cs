using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.Features.Recipes.Common;

public sealed record RecipeNutritionRequest(
    int? Calories,
    decimal? Protein,
    decimal? Carbohydrates,
    decimal? Fat,
    decimal? Fiber,
    decimal? Sodium);

public sealed record CreateRecipeStepRequest(
    string Title,
    string Description,
    int? TimerMinutes,
    string? ImageUrl);

public sealed record CreateRecipeIngredientRequest(
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes);

public sealed record CreateRecipeDraftRequest(
    string Title,
    string? Description,
    Guid? CategoryId,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty,
    string? Instructions,
    RecipeNutritionRequest? Nutrition,
    IReadOnlyList<CreateRecipeStepRequest>? Steps,
    IReadOnlyList<CreateRecipeIngredientRequest>? Ingredients);

public sealed record UpdateRecipeRequest(
    string Title,
    string Description,
    string? Instructions,
    Guid? CategoryId,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty,
    RecipeNutritionRequest? Nutrition,
    byte[] RowVersion);

public sealed record RecipeDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? Instructions,
    Guid? CategoryId,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty,
    RecipeStatus Status,
    string AuthorId,
    byte[] RowVersion);

public sealed record AddRecipeStepRequest(
    string Title,
    string Description,
    int? TimerMinutes,
    string? ImageUrl);

public sealed record UpdateRecipeStepRequest(
    string Title,
    string Description,
    int? TimerMinutes,
    string? ImageUrl);

public sealed record RecipeStepResponseDto(
    Guid Id,
    Guid RecipeId,
    int StepNumber,
    string Title,
    string Description,
    int? TimerMinutes,
    string? ImageUrl);

public sealed record AddIngredientRequest(
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes);

public sealed record UpdateIngredientRequest(
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes);

public sealed record RecipeIngredientResponseDto(
    Guid Id,
    Guid RecipeId,
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes,
    int OrderIndex);

public sealed record RecipeImageResponseDto(
    Guid Id,
    Guid RecipeId,
    string OriginalUrl,
    string? MediumUrl,
    string? ThumbnailUrl,
    bool IsPrimary,
    int OrderIndex);
