using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Domain.Entities;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Images.UploadImage;

public sealed record UploadRecipeImageCommand(
    Guid RecipeId,
    Stream FileStream,
    string OriginalFileName,
    string? ContentType) : IRequest<RecipeImageResponseDto>;

public sealed class UploadRecipeImageCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    ISupabaseStorageService storageService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<UploadRecipeImageCommand, RecipeImageResponseDto>
{
    public async Task<RecipeImageResponseDto> Handle(UploadRecipeImageCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdWithDetailsAsync(command.RecipeId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.RecipeId}'.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        // 1. Đọc stream vào bộ nhớ đệm để kiểm tra Magic Bytes (tối thiểu 12 bytes)
        using var memoryStream = new MemoryStream();
        await command.FileStream.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();

        if (bytes.Length == 0)
        {
            throw new UnprocessableEntityException("EMPTY_FILE", "Tệp tin hình ảnh không được để trống.");
        }

        // 2. Thẩm duyệt Magic Bytes nhị phân (JPEG, PNG, WebP)
        var (isValid, extension, detectedContentType) = ValidateMagicBytes(bytes);
        if (!isValid)
        {
            throw new UnprocessableEntityException(
                "INVALID_IMAGE_FORMAT",
                "Định dạng tệp không hợp lệ. Hệ thống chỉ chấp nhận hình ảnh định dạng JPEG, PNG hoặc WebP.");
        }

        // 3. Chống Path Traversal: Loại bỏ hoàn toàn tên file người dùng gửi lên, sinh tên GUID ngẫu nhiên
        var safeFileName = $"recipes/{command.RecipeId}/{Guid.NewGuid()}{extension}";

        // 4. Tải lên Supabase Storage bucket 'culinary-blog'
        memoryStream.Position = 0;
        var publicUrl = await storageService.UploadFileAsync(
            memoryStream,
            safeFileName,
            detectedContentType,
            cancellationToken);

        // 5. Thêm bản ghi RecipeImage vào CSDL (tự động gán IsPrimary = true cho ảnh đầu tiên)
        var isPrimary = !recipe.Images.Any(img => !img.IsDeleted && img.IsPrimary);
        var nextOrderIndex = recipe.Images.Count(img => !img.IsDeleted) + 1;

        var recipeImage = new RecipeImage
        {
            RecipeId = recipe.Id,
            OriginalUrl = publicUrl,
            IsPrimary = isPrimary,
            OrderIndex = nextOrderIndex
        };

        recipe.AddImage(recipeImage);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateListAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);

        return new RecipeImageResponseDto(
            recipeImage.Id,
            recipeImage.RecipeId,
            recipeImage.OriginalUrl,
            recipeImage.MediumUrl,
            recipeImage.ThumbnailUrl,
            recipeImage.IsPrimary,
            recipeImage.OrderIndex);
    }

    private static (bool IsValid, string Extension, string ContentType) ValidateMagicBytes(byte[] bytes)
    {
        // JPEG: FF D8 FF
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return (true, ".jpg", "image/jpeg");
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return (true, ".png", "image/png");
        }

        // WebP: RIFF .... WEBP
        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 && // RIFF
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) // WEBP
        {
            return (true, ".webp", "image/webp");
        }

        return (false, string.Empty, string.Empty);
    }
}
