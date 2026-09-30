using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.Features.Recipes.GetBySlug;

public sealed record RecipeDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? Instructions,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int TotalTimeMinutes,
    int Servings,
    RecipeDifficulty Difficulty,
    RecipeStatus Status,
    DateTime DatePublished,
    string AuthorId,
    RecipeAuthorDto Author,
    RecipeCategoryDto? Category,
    IReadOnlyList<RecipeStepDto> Steps,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeImageDto> Images,
    RecipeNutritionDto Nutrition);

public sealed record RecipeAuthorDto(string Name, string? AvatarUrl);
public sealed record RecipeCategoryDto(Guid Id, string Name, string Slug);
public sealed record RecipeStepDto(int StepNumber, string Title, string Description, int? TimerMinutes, string? ImageUrl);
public sealed record RecipeIngredientDto(string Name, decimal? Quantity, string? Unit, string? Notes, int OrderIndex);
public sealed record RecipeImageDto(string OriginalUrl, string? MediumUrl, string? ThumbnailUrl, bool IsPrimary, int OrderIndex);
public sealed record RecipeNutritionDto(int? Calories, decimal? Protein, decimal? Carbohydrates, decimal? Fat, decimal? Fiber, decimal? Sodium);
