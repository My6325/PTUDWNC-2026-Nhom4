using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Categories.Queries.GetCategoryBySlug;

/// <summary>
/// Handler xử lý GetCategoryBySlugQuery tra cứu thông tin danh mục theo slug URL và tải kèm các bài viết liên quan.
/// </summary>
public class GetCategoryBySlugQueryHandler : IRequestHandler<GetCategoryBySlugQuery, CategoryDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCategoryBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDetailDto?> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
        {
            return null;
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        // 1. Tìm thông tin danh mục theo Slug
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == normalizedSlug && !c.IsDeleted, cancellationToken);

        if (category is null)
        {
            return null;
        }

        // 2. Nạp danh sách tóm tắt các món ăn Published đang trực thuộc danh mục này
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

        // 3. Trả về DTO chi tiết
        return new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.OrderIndex,
            recipes
        );
    }
}
