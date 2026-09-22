using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Contracts;

public interface IRecipeRepository
{
    Task<PaginatedResult<Recipe>> SearchRecipesAsync(
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken);
}