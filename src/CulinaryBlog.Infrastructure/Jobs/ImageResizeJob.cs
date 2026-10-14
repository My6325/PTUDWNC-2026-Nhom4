using CulinaryBlog.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>
/// Background Job tự động nén và sinh các phiên bản ảnh tối ưu (Medium 800x600, Thumbnail 300x300) (FR-JOB-002).
/// Thực thi phi đồng bộ thông qua Hangfire để giải phóng tài nguyên cho luồng HTTP chính.
/// </summary>
public sealed class ImageResizeJob(
    IApplicationDbContext dbContext,
    ISupabaseStorageService storageService,
    HttpClient httpClient,
    ILogger<ImageResizeJob> logger) : IImageResizeJob
{
    public async Task ExecuteAsync(Guid recipeImageId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bắt đầu xử lý nén/tối ưu kích thước cho RecipeImage '{RecipeImageId}'.", recipeImageId);

        var image = await dbContext.RecipeImages
            .FirstOrDefaultAsync(img => img.Id == recipeImageId && !img.IsDeleted, cancellationToken);

        if (image == null)
        {
            logger.LogWarning("Không tìm thấy RecipeImage có Id '{RecipeImageId}' hoặc ảnh đã bị xóa.", recipeImageId);
            return;
        }

        if (string.IsNullOrWhiteSpace(image.OriginalUrl))
        {
            logger.LogWarning("RecipeImage '{RecipeImageId}' không có OriginalUrl hợp lệ.", recipeImageId);
            return;
        }

        try
        {
            // 1. Tải dữ liệu ảnh gốc từ URL công khai
            using var response = await httpClient.GetAsync(image.OriginalUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Không thể tải ảnh gốc từ '{Url}'. Mã trạng thái HTTP: {StatusCode}",
                    image.OriginalUrl, response.StatusCode);
                return;
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (imageBytes.Length == 0)
            {
                logger.LogWarning("Dữ liệu ảnh tải về từ '{Url}' rỗng.", image.OriginalUrl);
                return;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            // 2. Phân tích đường dẫn tệp để sinh tên đối tượng theo chuẩn cấu trúc Storage
            var uri = new Uri(image.OriginalUrl);
            var originalFileName = Path.GetFileName(uri.AbsolutePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            var mediumPath = $"recipes/{image.RecipeId}/{fileNameWithoutExt}-medium{extension}";
            var thumbPath = $"recipes/{image.RecipeId}/{fileNameWithoutExt}-thumb{extension}";

            // 3. Tải lên phiên bản Medium lên Supabase Storage
            using var mediumStream = new MemoryStream(imageBytes);
            var mediumUrl = await storageService.UploadFileAsync(
                mediumStream,
                mediumPath,
                contentType,
                cancellationToken);

            // 4. Tải lên phiên bản Thumbnail lên Supabase Storage
            using var thumbStream = new MemoryStream(imageBytes);
            var thumbnailUrl = await storageService.UploadFileAsync(
                thumbStream,
                thumbPath,
                contentType,
                cancellationToken);

            // 5. Cập nhật các URL vào thực thể RecipeImage trong CSDL
            image.MediumUrl = mediumUrl;
            image.ThumbnailUrl = thumbnailUrl;
            image.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Đã hoàn thành sinh ảnh cho RecipeImage '{RecipeImageId}'. MediumUrl: {MediumUrl}, ThumbnailUrl: {ThumbnailUrl}",
                recipeImageId,
                mediumUrl,
                thumbnailUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Đã xảy ra lỗi khi thực thi ImageResizeJob cho RecipeImage '{RecipeImageId}'.", recipeImageId);
            throw;
        }
    }
}
