using Microsoft.AspNetCore.Identity;

namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Thực thể người dùng ứng dụng, mở rộng từ <see cref="IdentityUser"/> của ASP.NET Core Identity.
/// Sử dụng khóa chính kiểu <c>string</c> (GUID dạng chuỗi) mặc định của Identity,
/// khớp với bảng APPLICATION_USER trong ERD (SPEC mục 3.4).
/// Bổ sung các trường hồ sơ (Profile) phục vụ hiển thị trên blog ẩm thực.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Tên hiển thị công khai của người dùng trên blog (bắt buộc).
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Đường dẫn URL tới ảnh đại diện (Avatar) của người dùng.
    /// Có thể null nếu người dùng chưa cập nhật ảnh đại diện.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Tiểu sử ngắn gọn giới thiệu bản thân người dùng.
    /// Có thể null nếu người dùng chưa cập nhật tiểu sử.
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Trạng thái hoạt động của tài khoản.
    /// Giá trị <c>false</c> nghĩa là tài khoản đã bị khóa/vô hiệu hóa bởi quản trị viên.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Thời điểm tạo tài khoản (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
