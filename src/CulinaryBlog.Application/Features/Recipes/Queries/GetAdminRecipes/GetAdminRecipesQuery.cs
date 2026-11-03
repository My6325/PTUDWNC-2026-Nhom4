using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Enums;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetAdminRecipes;

/// <summary>
/// Query lấy danh sách công thức phân trang phục vụ trang quản trị Admin,
/// hỗ trợ lọc theo trạng thái (Draft, Published, Archived), thùng rác (Deleted) và từ khóa tìm kiếm.
/// </summary>
public sealed class GetAdminRecipesQuery : IRequest<PaginatedResult<AdminRecipeListDto>>
{
    public string? SearchTerm { get; init; }

    public RecipeStatus? Status { get; init; }

    public bool? OnlyDeleted { get; init; } = false;

    public int? PageIndex { get; init; } = 1;

    public int? PageSize { get; init; } = 15;
}
