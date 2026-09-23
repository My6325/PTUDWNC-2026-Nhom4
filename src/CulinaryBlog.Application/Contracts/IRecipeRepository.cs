using CulinaryBlog.Domain.Common;
using CulinaryBlog.Application.Features.Recipes.Queries;

namespace CulinaryBlog.Application.Contracts;

public interface IRecipeRepository
{
    Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
        string? searchTerm,
        Guid? categoryId,
        string? difficulty,
        string sortBy,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken);
}
