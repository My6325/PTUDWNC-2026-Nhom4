using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Domain.Entities;

public class Recipe : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Instructions { get; set; }

    public int PrepTimeMinutes { get; set; }

    public int CookTimeMinutes { get; set; }

    public int Servings { get; set; }

    public RecipeDifficulty Difficulty { get; set; } = RecipeDifficulty.Easy;

    public RecipeStatus Status { get; set; } = RecipeStatus.Draft;

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public RecipeNutrition Nutrition { get; set; } = new();

    public ICollection<RecipeStep> Steps { get; private set; } = new List<RecipeStep>();

    public ICollection<RecipeIngredient> Ingredients { get; private set; } = new List<RecipeIngredient>();

    public ICollection<RecipeImage> Images { get; private set; } = new List<RecipeImage>();

    public static Recipe Create(
        string title,
        string slug,
        string? description,
        string? instructions,
        Guid? categoryId,
        string authorId,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        RecipeDifficulty difficulty)
    {
        return new Recipe
        {
            Title = title.Trim(),
            Slug = slug,
            Description = description?.Trim(),
            Instructions = instructions?.Trim(),
            CategoryId = categoryId,
            AuthorId = authorId,
            PrepTimeMinutes = prepTimeMinutes,
            CookTimeMinutes = cookTimeMinutes,
            Servings = servings,
            Difficulty = difficulty,
            Status = RecipeStatus.Draft
        };
    }

    public void Update(
        string title,
        string slug,
        string description,
        string? instructions,
        Guid? categoryId,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int servings,
        RecipeDifficulty difficulty)
    {
        Title = title.Trim();
        Slug = slug;
        Description = description.Trim();
        Instructions = instructions?.Trim();
        CategoryId = categoryId;
        PrepTimeMinutes = prepTimeMinutes;
        CookTimeMinutes = cookTimeMinutes;
        Servings = servings;
        Difficulty = difficulty;
    }

    public void SetNutrition(RecipeNutrition? nutrition)
    {
        Nutrition = nutrition ?? new RecipeNutrition();
    }

    public void AddStep(RecipeStep step)
    {
        Steps.Add(step);
    }

    public void AddIngredient(RecipeIngredient ingredient)
    {
        Ingredients.Add(ingredient);
    }
}
