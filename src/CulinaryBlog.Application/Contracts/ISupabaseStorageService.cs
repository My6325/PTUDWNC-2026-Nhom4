namespace CulinaryBlog.Application.Contracts;

/// <summary>
/// Dịch vụ lưu trữ tệp đa phương tiện tích hợp Supabase Storage (FR-FILE-001, FR-FILE-002).
/// </summary>
public interface ISupabaseStorageService
{
    /// <summary>
    /// Tải tệp lên Supabase Storage bucket 'culinary-blog'.
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa tệp khỏi Supabase Storage.
    /// </summary>
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
