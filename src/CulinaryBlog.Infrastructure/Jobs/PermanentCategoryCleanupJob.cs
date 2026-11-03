using CulinaryBlog.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>
/// Background Job tự động quét và dọn dẹp vĩnh viễn (Hard Delete) các danh mục trong Thùng rác đã quá 30 ngày (FR-CAT-005).
/// Định kỳ thực thi lúc 03:00 AM UTC hàng ngày.
/// </summary>
public sealed class PermanentCategoryCleanupJob(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork,
    IMemoryCache memoryCache,
    ILogger<PermanentCategoryCleanupJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var retentionThreshold = DateTime.UtcNow.AddDays(-30);

        logger.LogInformation("Bắt đầu quét dọn dẹp các danh mục trong Thùng rác bị xóa trước {Threshold}.", retentionThreshold);

        var expiredCategories = await dbContext.Categories
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted && c.DeletedAt != null && c.DeletedAt < retentionThreshold)
            .ToListAsync(cancellationToken);

        if (expiredCategories.Count == 0)
        {
            logger.LogInformation("Không có danh mục nào trong Thùng rác quá hạn 30 ngày cần xóa vĩnh viễn.");
            return;
        }

        foreach (var category in expiredCategories)
        {
            // 1. Gỡ CategoryId trên các món ăn liên quan
            var linkedRecipes = await dbContext.Recipes
                .IgnoreQueryFilters()
                .Where(r => r.CategoryId == category.Id)
                .ToListAsync(cancellationToken);

            foreach (var recipe in linkedRecipes)
            {
                recipe.CategoryId = null;
            }

            // 2. Dọn dẹp SlugRedirects liên kết (an toàn lỗi nếu bảng chưa tồn tại)
            try
            {
                var linkedRedirects = await dbContext.SlugRedirects
                    .Where(s => s.EntityId == category.Id && s.EntityType == "Category")
                    .ToListAsync(cancellationToken);

                if (linkedRedirects.Count > 0)
                {
                    dbContext.SlugRedirects.RemoveRange(linkedRedirects);
                }
            }
            catch
            {
                // Bỏ qua nếu bảng SlugRedirects chưa được tạo trên CSDL
            }

            // 3. Xóa vật lý danh mục
            dbContext.Categories.Remove(category);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Vô hiệu hóa Cache
        memoryCache.Remove("categories:all");

        logger.LogInformation("Đã dọn dẹp vĩnh viễn {Count} danh mục ẩm thực hết hạn lưu trữ 30 ngày.", expiredCategories.Count);
    }
}
