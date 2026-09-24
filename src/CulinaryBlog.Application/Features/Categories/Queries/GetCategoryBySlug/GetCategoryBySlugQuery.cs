using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Queries.GetCategoryBySlug;

/// <summary>
/// Query tìm kiếm chi tiết danh mục ẩm thực theo slug kèm các món ăn tóm tắt thuộc danh mục (FR-CAT-002).
/// </summary>
public record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryDetailDto?>;
