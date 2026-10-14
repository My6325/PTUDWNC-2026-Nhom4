namespace CulinaryBlog.Application.Contracts;

/// <summary>
/// Hợp đồng cho Background Job nén và tối ưu kích thước hình ảnh công thức (FR-JOB-002).
/// Sinh các phiên bản Medium (800x600) và Thumbnail (300x300) đẩy lên Supabase Storage.
/// </summary>
public interface IImageResizeJob
{
    /// <summary>
    /// Thực thi tác vụ xử lý kích thước ảnh trong background.
    /// </summary>
    /// <param name="recipeImageId">Định danh bản ghi ảnh công thức cần xử lý.</param>
    /// <param name="cancellationToken">Token hủy tác vụ nền.</param>
    Task ExecuteAsync(Guid recipeImageId, CancellationToken cancellationToken = default);
}
