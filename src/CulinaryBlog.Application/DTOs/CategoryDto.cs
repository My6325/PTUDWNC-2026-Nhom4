namespace CulinaryBlog.Application.DTOs;

/// <summary>
/// Data Transfer Object cho danh sách danh mục ẩm thực (FR-CAT-001).
/// Bổ sung CreatedAt, UpdatedAt và DeletedAt phục vụ quản trị Admin theo dõi lịch sử và Thùng rác.
/// </summary>
public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int OrderIndex,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null,
    DateTime? DeletedAt = null
);
