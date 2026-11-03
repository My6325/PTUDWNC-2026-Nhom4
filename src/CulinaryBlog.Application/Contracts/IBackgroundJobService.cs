namespace CulinaryBlog.Application.Contracts;

/// <summary>
/// Abstraction điều phối các tác vụ chạy nền (Background Jobs).
/// Cho phép đẩy tác vụ vào hàng đợi mà không làm chậm hoặc làm lỗi luồng nghiệp vụ chính.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Đưa tác vụ gửi email chào mừng người dùng mới vào hàng đợi nền.
    /// </summary>
    /// <param name="userId">Id của người dùng vừa đăng ký thành công.</param>
    /// <returns>Mã nhận diện công việc (Job ID hoặc trạng thái).</returns>
    string EnqueueWelcomeEmail(string userId);

    /// <summary>
    /// Đưa tác vụ nén và tạo thumbnail đa kích thước (Medium 800x600, Thumb 300x300) vào hàng đợi nền (FR-JOB-002).
    /// </summary>
    /// <param name="recipeImageId">Id bản ghi ảnh công thức.</param>
    /// <returns>Mã nhận diện công việc (Job ID hoặc trạng thái).</returns>
    string EnqueueImageResize(Guid recipeImageId) => string.Empty;
}
