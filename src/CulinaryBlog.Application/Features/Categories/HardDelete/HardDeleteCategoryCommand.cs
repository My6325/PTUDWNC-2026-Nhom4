using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Categories.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Features.Categories.HardDelete;

/// <summary>
/// Command xóa vĩnh viễn danh mục ẩm thực khỏi cơ sở dữ liệu (Hard Delete).
/// Chỉ dành riêng cho Quản trị viên (Admin) để dọn dẹp triệt để dữ liệu rác.
/// </summary>
public sealed record HardDeleteCategoryCommand(Guid Id) : IRequest;

public sealed class HardDeleteCategoryCommandHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork,
    IMemoryCache memoryCache)
    : IRequestHandler<HardDeleteCategoryCommand>
{
    public async Task Handle(HardDeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        // 1. Tìm danh mục bất kể đã bị xóa mềm hay chưa (IgnoreQueryFilters)
        var category = await dbContext.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy danh mục với Id '{command.Id}'.");

        // 2. Gỡ bỏ liên kết CategoryId khỏi toàn bộ các món ăn liên quan (set về null để không làm mất món ăn)
        var linkedRecipes = await dbContext.Recipes
            .IgnoreQueryFilters()
            .Where(r => r.CategoryId == category.Id)
            .ToListAsync(cancellationToken);

        foreach (var recipe in linkedRecipes)
        {
            recipe.CategoryId = null;
        }

        // 3. Dọn dẹp sạch sẽ toàn bộ các SlugRedirects liên kết với Category này (an toàn lỗi nếu bảng chưa tồn tại)
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

        // 4. Xóa vật lý bản ghi Category khỏi cơ sở dữ liệu
        dbContext.Categories.Remove(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Vô hiệu hóa In-Memory Cache
        memoryCache.Remove(CategoryCacheKeys.All);
    }
}
