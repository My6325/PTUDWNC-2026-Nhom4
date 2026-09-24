namespace CulinaryBlog.Application.DTOs;

/// <summary>
/// Data Transfer Object biểu diễn thông tin tóm tắt của công thức nấu ăn hiển thị trong trang danh mục.
/// Chỉ chứa các trường xem nhanh (Title, Slug, Thời gian, Độ khó, Ảnh chính), không nạp bảng con chuyên sâu.
/// </summary>
public record CategoryRecipeSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    int Servings,
    string Difficulty,
    string? PrimaryImageUrl
);
