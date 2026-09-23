using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

public class RecipeStep : BaseEntity
{
    public Guid RecipeId { get; set; }

    public int StepNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? TimerMinutes { get; set; }

    public string? ImageUrl { get; set; }

    public static RecipeStep Create(
        Guid recipeId,
        int stepNumber,
        string title,
        string description,
        int? timerMinutes,
        string? imageUrl)
    {
        return new RecipeStep
        {
            RecipeId = recipeId,
            StepNumber = stepNumber,
            Title = title.Trim(),
            Description = description.Trim(),
            TimerMinutes = timerMinutes,
            ImageUrl = imageUrl?.Trim()
        };
    }
}
