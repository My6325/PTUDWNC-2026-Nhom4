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
}
