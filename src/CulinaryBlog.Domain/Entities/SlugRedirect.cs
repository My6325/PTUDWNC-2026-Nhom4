using System;

namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Thực thể lưu vết lịch sử Slug của Danh mục (hoặc Bài viết) để hỗ trợ điều hướng HTTP 301 Moved Permanently.
/// Giúp bảo toàn thứ hạng SEO và trải nghiệm người dùng khi tên/đường dẫn danh mục thay đổi.
/// </summary>
public class SlugRedirect
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Id của thực thể (ví dụ CategoryId hoặc RecipeId).
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Loại thực thể ("Category" hoặc "Recipe").
    /// </summary>
    public string EntityType { get; set; } = "Category";

    /// <summary>
    /// Slug cũ trước khi cập nhật.
    /// </summary>
    public string OldSlug { get; set; } = string.Empty;

    /// <summary>
    /// Thời điểm phát sinh điều hướng.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
