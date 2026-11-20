using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Common;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Features.Categories.Queries.GetCategories;

/// <summary>
/// Handler đọc danh mục công khai và cache kết quả trong 60 phút kể từ lần truy cập gần nhất.
/// </summary>
public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    public GetCategoriesQueryHandler(IApplicationDbContext context, IMemoryCache memoryCache)
    {
        _context = context;
        _memoryCache = memoryCache;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra dữ liệu trong In-Memory Cache
        if (_memoryCache.TryGetValue(CategoryCacheKeys.All, out List<CategoryDto>? cachedCategories) && cachedCategories is not null)
        {
            return cachedCategories;
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.OrderIndex)
            .ThenBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description, c.ImageUrl,
                c.OrderIndex, c.CreatedAt, c.UpdatedAt, c.DeletedAt)
            {
                RecipeCount = _context.Recipes.Count(r =>
                    r.CategoryId == c.Id && r.Status == RecipeStatus.Published && !r.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        // 3. Lưu vào Cache với thời hạn 60 phút
        _memoryCache.Set(CategoryCacheKeys.All, categories, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheDuration
        });

        return categories;
    }
}
