using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Contracts;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Recipe?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Recipe?>(null);

    Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken) => GetByIdAsync(id, cancellationToken);

    Task<Recipe?> GetByIdForPublishingAsync(Guid id, CancellationToken cancellationToken);

    Task<RecipeDetailDto?> GetDetailBySlugAsync(string slug, CancellationToken cancellationToken);

    Task AddAsync(Recipe recipe, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludedRecipeId, CancellationToken cancellationToken);

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

    Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
        string? searchTerm, Guid? categoryId, string? difficulty,
        int? minCookTimeMinutes, int? maxCookTimeMinutes, int? minServings, int? maxServings,
        string sortBy, int pageIndex, int pageSize, CancellationToken cancellationToken) =>
        SearchRecipesAsync(searchTerm, categoryId, difficulty, sortBy, pageIndex, pageSize, cancellationToken);

    Task<CulinaryBlog.Domain.Common.PaginatedResult<CulinaryBlog.Application.Features.Recipes.Queries.GetAdminRecipes.AdminRecipeListDto>> GetAdminRecipesAsync(
        string? searchTerm,
        CulinaryBlog.Domain.Enums.RecipeStatus? status,
        bool onlyDeleted,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken);
}
