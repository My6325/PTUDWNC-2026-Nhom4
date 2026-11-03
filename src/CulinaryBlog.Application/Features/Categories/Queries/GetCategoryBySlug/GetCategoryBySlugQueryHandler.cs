using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Categories.Queries.GetCategoryBySlug;

/// <summary>
/// Handler xử lý GetCategoryBySlugQuery tra cứu thông tin danh mục theo slug URL và tải kèm các bài viết liên quan.
/// Hỗ trợ tra cứu ngược từ SlugRedirects để phát tín hiệu HTTP 301 Moved Permanently.
/// </summary>
public class GetCategoryBySlugQueryHandler : IRequestHandler<GetCategoryBySlugQuery, CategorySlugLookupResult?>
{
    private readonly IApplicationDbContext _context;

    public GetCategoryBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategorySlugLookupResult?> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return null;
        }

        var rawSlug = request.Slug.Trim();
        var normalizedSlug = rawSlug.ToLowerInvariant();
        var generatedSlug = SlugHelper.GenerateSlug(rawSlug);

        // 1. Tìm thông tin danh mục theo Slug (hỗ trợ cả slug kebab-case lẫn chuỗi tiếng Việt có dấu tự động chuyển đổi)
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => (c.Slug == normalizedSlug || c.Slug == generatedSlug) && !c.IsDeleted, cancellationToken);

        // 2. Nếu không tìm thấy trong Categories đang hoạt động, kiểm tra bảng SlugRedirects xem có phải slug cũ không
        if (category is null)
        {
            try
            {
                var redirect = await _context.SlugRedirects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => (s.OldSlug == normalizedSlug || s.OldSlug == generatedSlug) && s.EntityType == "Category", cancellationToken);

                if (redirect is not null)
                {
                    var activeCategory = await _context.Categories
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == redirect.EntityId && !c.IsDeleted, cancellationToken);

                    if (activeCategory is not null)
                    {
                        // Phát hiện slug cũ -> Trả về slug đích để API thực hiện HTTP 301 Redirect
                        return new CategorySlugLookupResult(RedirectToSlug: activeCategory.Slug);
                    }
                }
            }
            catch
            {
                // Bỏ qua nếu bảng SlugRedirects chưa được tạo trên CSDL
            }

            return null;
        }

        // 3. Nạp danh sách tóm tắt các món ăn Published đang trực thuộc danh mục này
        var recipes = await _context.Recipes
            .AsNoTracking()
            .Where(r => r.CategoryId == category.Id && r.Status == RecipeStatus.Published && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new CategoryRecipeSummaryDto(
                r.Id,
                r.Title,
                r.Slug,
                r.Description,
                r.PrepTimeMinutes,
                r.CookTimeMinutes,
                r.Servings,
                r.Difficulty.ToString(),
                _context.RecipeImages
                    .Where(img => img.RecipeId == r.Id && img.IsPrimary && !img.IsDeleted)
                    .Select(img => img.MediumUrl ?? img.OriginalUrl)
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);

        // 4. Trả về DTO chi tiết hoàn chỉnh
        var detail = new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.OrderIndex,
            recipes
        );

        return new CategorySlugLookupResult(Category: detail);
    }
}
