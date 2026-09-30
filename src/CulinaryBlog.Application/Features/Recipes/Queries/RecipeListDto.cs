using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

public sealed class RecipeListDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? CoverImageUrl { get; init; }

    public string? CategoryName { get; init; }

    public string AuthorName { get; init; } = string.Empty;

    public RecipeDifficulty Difficulty { get; init; }

    public DateTime CreatedAt { get; init; }
}
