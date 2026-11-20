namespace CulinaryBlog.Application.DTOs;

/// <summary>
/// Data Transfer Object biểu diễn chi tiết danh mục ẩm thực kèm danh sách bài viết liên quan (FR-CAT-002).
/// </summary>
public record CategoryDetailDto(
    CategoryDto Category,
    CategoryRecipePage Recipes
);

public record CategoryRecipePage(
    IReadOnlyList<CategoryRecipeSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
