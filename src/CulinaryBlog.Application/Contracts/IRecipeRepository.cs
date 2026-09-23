using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Contracts;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Recipe recipe, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludedRecipeId, CancellationToken cancellationToken);

    Task<PaginatedResult<Recipe>> SearchRecipesAsync(
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken);
}
