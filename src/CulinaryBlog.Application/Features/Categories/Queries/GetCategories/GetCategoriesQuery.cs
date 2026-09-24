using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Queries.GetCategories;

/// <summary>
/// Query lấy toàn bộ danh mục ẩm thực phục vụ menu điều hướng và trang danh mục (FR-CAT-001).
/// </summary>
public record GetCategoriesQuery : IRequest<List<CategoryDto>>;
