namespace CulinaryBlog.Domain.Exceptions;

/// <summary>
/// Ngoại lệ ném ra khi tên danh mục để trống hoặc vượt quá độ dài tối đa (100 ký tự).
/// </summary>
public sealed class InvalidCategoryNameException(string message = "Tên danh mục không được để trống hoặc vượt quá 100 ký tự.")
    : DomainException(message, "INVALID_CATEGORY_NAME");

/// <summary>
/// Ngoại lệ ném ra khi thứ tự hiển thị của danh mục là số âm.
/// </summary>
public sealed class InvalidCategoryOrderIndexException(string message = "Thứ tự hiển thị của danh mục phải lớn hơn hoặc bằng 0.")
    : DomainException(message, "INVALID_CATEGORY_ORDER_INDEX");

/// <summary>
/// Ngoại lệ ném ra khi đường dẫn định danh (Slug) của danh mục bị để trống.
/// </summary>
public sealed class CategorySlugEmptyException(string message = "Đường dẫn định danh (Slug) của danh mục không được để trống.")
    : DomainException(message, "CATEGORY_SLUG_EMPTY");
