using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using MediatR;

namespace CulinaryBlog.Application.Features.Categories.Queries.GetTrashCategories;

/// <summary>
/// Query lấy danh sách các danh mục trong Thùng rác (đã bị xóa mềm) dành cho Admin.
/// </summary>
public record GetTrashCategoriesQuery : IRequest<List<CategoryDto>>;

public class GetTrashCategoriesQueryHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<GetTrashCategoriesQuery, List<CategoryDto>>
{
    public async Task<List<CategoryDto>> Handle(GetTrashCategoriesQuery request, CancellationToken cancellationToken)
    {
        var deletedCategories = await categoryRepository.GetDeletedCategoriesAsync(cancellationToken);

        return deletedCategories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.ImageUrl,
            c.OrderIndex,
            c.CreatedAt,
            c.UpdatedAt,
            c.DeletedAt
        )).ToList();
    }
}
