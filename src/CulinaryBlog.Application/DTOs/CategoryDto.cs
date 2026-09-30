namespace CulinaryBlog.Application.DTOs;

/// <summary>
/// Data Transfer Object cho danh sách danh mục ẩm thực (FR-CAT-001).
/// </summary>
public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int OrderIndex
);
