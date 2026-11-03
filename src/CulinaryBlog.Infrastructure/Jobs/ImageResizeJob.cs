using CulinaryBlog.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>
/// Background Job tự động nén và sinh các phiên bản ảnh tối ưu (Medium 800x600, Thumbnail 300x300) (FR-JOB-002).
/// Thực thi phi đồng bộ thông qua Hangfire hoặc Fallback Runner để giải phóng tài nguyên cho luồng HTTP chính.
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

            // 2. Phân tích đường dẫn tệp để sinh tên đối tượng theo chuẩn cấu trúc Storage
            var uri = new Uri(image.OriginalUrl);
            var originalFileName = Path.GetFileName(uri.AbsolutePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);

            var mediumPath = $"recipes/{image.RecipeId}/{fileNameWithoutExt}-medium.webp";
            var thumbPath = $"recipes/{image.RecipeId}/{fileNameWithoutExt}-thumb.webp";

            // 3. Sử dụng SixLabors.ImageSharp để nén và resize chuẩn xác
            using var loadedImage = Image.Load(imageBytes);

            // 3.1 Phiên bản Medium: Max 800x600 (giữ nguyên tỷ lệ, nén WebP chất lượng 80%)
            using var mediumImg = loadedImage.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(800, 600)
            }));
            using var mediumStream = new MemoryStream();
            await mediumImg.SaveAsWebpAsync(mediumStream, new WebpEncoder { Quality = 80 }, cancellationToken);
            mediumStream.Position = 0;

            var mediumUrl = await storageService.UploadFileAsync(
                mediumStream,
                mediumPath,
                "image/webp",
                cancellationToken);

            // 3.2 Phiên bản Thumbnail: Vuông 300x300 (Crop trung tâm làm thumbnail)
            using var thumbImg = loadedImage.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Size = new Size(300, 300)
            }));
            using var thumbStream = new MemoryStream();
            await thumbImg.SaveAsWebpAsync(thumbStream, new WebpEncoder { Quality = 80 }, cancellationToken);
            thumbStream.Position = 0;

            var thumbnailUrl = await storageService.UploadFileAsync(
                thumbStream,
                thumbPath,
                "image/webp",
                cancellationToken);

            // 4. Cập nhật các URL vào thực thể RecipeImage trong CSDL
            image.MediumUrl = mediumUrl;
            image.ThumbnailUrl = thumbnailUrl;
            image.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Đã hoàn thành sinh ảnh nén WebP cho RecipeImage '{RecipeImageId}'. MediumUrl: {MediumUrl}, ThumbnailUrl: {ThumbnailUrl}",
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
