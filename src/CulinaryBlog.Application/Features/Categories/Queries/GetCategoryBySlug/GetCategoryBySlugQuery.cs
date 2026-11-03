using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Queries.GetCategoryBySlug;

/// <summary>
/// Kết quả tra cứu Category theo Slug:
/// - Nếu Slug hiện tại khớp: Category != null
/// - Nếu là Slug cũ cần điều hướng HTTP 301: RedirectToSlug != null
/// - Nếu hoàn toàn không tồn tại: trả về null
/// </summary>
public record CategorySlugLookupResult(
    CategoryDetailDto? Category = null,
    string? RedirectToSlug = null
);

/// <summary>
/// Query tìm kiếm chi tiết danh mục ẩm thực theo slug kèm các món ăn tóm tắt thuộc danh mục (FR-CAT-002 & SlugRedirects).
/// </summary>
public record GetCategoryBySlugQuery(string Slug) : IRequest<CategorySlugLookupResult?>;
