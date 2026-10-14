using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Domain.Exceptions;

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


    public DateTime? PublishedAt { get; private set; }

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
        ValidateDetails(title, prepTimeMinutes, cookTimeMinutes);

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
        ValidateDetails(title, prepTimeMinutes, cookTimeMinutes);

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

    public void ChangeStatus(RecipeStatus targetStatus)
    {
        var isValidTransition = (Status, targetStatus) switch
        {
            (RecipeStatus.Draft, RecipeStatus.Published) => true,
            (RecipeStatus.Published, RecipeStatus.Archived) => true,
            (RecipeStatus.Published, RecipeStatus.Draft) => true,
            (RecipeStatus.Archived, RecipeStatus.Published) => true,
            (RecipeStatus.Archived, RecipeStatus.Draft) => true,
            _ => false
        };

        if (!isValidTransition)
        {
            throw new InvalidRecipeStatusTransitionException(Status, targetStatus);
        }

        Status = targetStatus;
        if (targetStatus == RecipeStatus.Published)
        {
            PublishedAt ??= DateTime.UtcNow;
        }
        else if (targetStatus == RecipeStatus.Draft)
        {
            PublishedAt = null;
        }
    }

    public void SetNutrition(RecipeNutrition? nutrition)
    {
        Nutrition = nutrition ?? new RecipeNutrition();
    }

    public void AddStep(RecipeStep step)
    {
        if (step.StepNumber <= 0)
        {
            step.StepNumber = Steps.Count(s => !s.IsDeleted) + 1;
        }
        Steps.Add(step);
    }

    public void RemoveStep(Guid stepId)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId && !s.IsDeleted);
        if (step is null) return;

        step.IsDeleted = true;
        step.DeletedAt = DateTime.UtcNow;

        RenumberSteps();
    }

    public void RenumberSteps()
    {
        var activeSteps = Steps.Where(s => !s.IsDeleted).OrderBy(s => s.StepNumber).ToList();
        for (int i = 0; i < activeSteps.Count; i++)
        {
            activeSteps[i].StepNumber = i + 1;
        }
    }

    public void AddIngredient(RecipeIngredient ingredient)
    {
        if (ingredient.OrderIndex <= 0)
        {
            ingredient.OrderIndex = Ingredients.Count(i => !i.IsDeleted) + 1;
        }
        Ingredients.Add(ingredient);
    }

    public void RemoveIngredient(Guid ingredientId)
    {
        var ingredient = Ingredients.FirstOrDefault(i => i.Id == ingredientId && !i.IsDeleted);
        if (ingredient is null) return;

        ingredient.IsDeleted = true;
        ingredient.DeletedAt = DateTime.UtcNow;

        RenumberIngredients();
    }

    public void RenumberIngredients()
    {
        var activeIngredients = Ingredients.Where(i => !i.IsDeleted).OrderBy(i => i.OrderIndex).ToList();
        for (int i = 0; i < activeIngredients.Count; i++)
        {
            activeIngredients[i].OrderIndex = i + 1;
        }
    }

    public void AddImage(RecipeImage image)
    {
        if (!Images.Any(img => !img.IsDeleted && img.IsPrimary))
        {
            image.IsPrimary = true;
        }
        if (image.OrderIndex <= 0)
        {
            image.OrderIndex = Images.Count(img => !img.IsDeleted) + 1;
        }
        Images.Add(image);
    }

    public void RemoveImage(Guid imageId)
    {
        var image = Images.FirstOrDefault(img => img.Id == imageId && !img.IsDeleted);
        if (image is null) return;

        image.IsDeleted = true;
        image.DeletedAt = DateTime.UtcNow;

        if (image.IsPrimary)
        {
            image.IsPrimary = false;
            var nextPrimary = Images.Where(img => !img.IsDeleted && img.Id != imageId)
                .OrderBy(img => img.OrderIndex)
                .FirstOrDefault();

            if (nextPrimary is not null)
            {
                nextPrimary.IsPrimary = true;
            }
        }
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;

        foreach (var step in Steps.Where(s => !s.IsDeleted))
        {
            step.IsDeleted = true;
            step.DeletedAt = DateTime.UtcNow;
        }

        foreach (var ingredient in Ingredients.Where(i => !i.IsDeleted))
        {
            ingredient.IsDeleted = true;
            ingredient.DeletedAt = DateTime.UtcNow;
        }

        foreach (var image in Images.Where(img => !img.IsDeleted))
        {
            image.IsDeleted = true;
            image.DeletedAt = DateTime.UtcNow;
        }
    }

    public void Publish()
    {
        if (!Steps.Any(step => !step.IsDeleted)
            || !Ingredients.Any(ingredient => !ingredient.IsDeleted)
            || !Images.Any(image => !image.IsDeleted && image.IsPrimary))
        {
            throw new RecipeNotEligibleForPublishException();
        }

        Status = RecipeStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    private static void ValidateDetails(string? title, int prepTimeMinutes, int cookTimeMinutes)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new EmptyRecipeTitleException();
        }

        if (prepTimeMinutes < 0)
        {
            throw new InvalidPreparationTimeException("preparation", prepTimeMinutes);
        }

        if (cookTimeMinutes < 0)
        {
            throw new InvalidPreparationTimeException("cooking", cookTimeMinutes);
        }
    }
}
