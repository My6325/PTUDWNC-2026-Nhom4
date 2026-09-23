using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Features.Categories.Queries.GetCategories;

/// <summary>
/// Handler xử lý GetCategoriesQuery với kỹ thuật In-Memory Cache (TTL 60 phút) và Mapster Projection.
/// </summary>
public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private const string CacheKey = "categories:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    public GetCategoriesQueryHandler(IApplicationDbContext context, IMemoryCache memoryCache)
    {
        _context = context;
        _memoryCache = memoryCache;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra dữ liệu trong In-Memory Cache
        if (_memoryCache.TryGetValue(CacheKey, out List<CategoryDto>? cachedCategories) && cachedCategories is not null)
        {
            return cachedCategories;
        }

        // 2. Nếu Cache miss, truy vấn Supabase PostgreSQL qua EF Core AsNoTracking & Mapster ProjectToType
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.OrderIndex)
            .ProjectToType<CategoryDto>()
            .ToListAsync(cancellationToken);

        // 3. Lưu vào Cache với thời hạn 60 phút
        _memoryCache.Set(CacheKey, categories, CacheDuration);

        return categories;
    }
}
