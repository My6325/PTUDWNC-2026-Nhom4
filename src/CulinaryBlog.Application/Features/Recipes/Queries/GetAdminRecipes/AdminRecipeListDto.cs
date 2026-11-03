using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetAdminRecipes;

/// <summary>
/// DTO đại diện cho một dòng công thức trên bảng Quản trị công thức dành riêng cho Admin (FR-RCP).
/// </summary>
public sealed class AdminRecipeListDto
{
    public byte[] RowVersion { get; init; } = [];

    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? CoverImageUrl { get; init; }

    public string? CategoryName { get; init; }

    public string AuthorName { get; init; } = string.Empty;

    public RecipeDifficulty Difficulty { get; init; }

    public RecipeStatus Status { get; init; }

    public bool IsDeleted { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? DeletedAt { get; init; }

    public DateTime? PublishedAt { get; init; }
}
