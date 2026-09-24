namespace CulinaryBlog.Application.DTOs;

/// <summary>
/// Data Transfer Object biểu diễn chi tiết danh mục ẩm thực kèm danh sách bài viết liên quan (FR-CAT-002).
/// </summary>
public record CategoryDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int OrderIndex,
    List<CategoryRecipeSummaryDto> Recipes
);
