using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Common;

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

    Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken);
}
