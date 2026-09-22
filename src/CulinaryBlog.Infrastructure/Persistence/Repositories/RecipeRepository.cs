using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Repositories;

public sealed class RecipeRepository : IRecipeRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RecipeRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResult<Recipe>> SearchRecipesAsync(
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be greater than zero.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
        }

        var query = _dbContext.Recipes
            .AsNoTracking()
            .AsQueryable();

        var normalizedSearchTerm = searchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            query = query.Where(recipe =>
                EF.Functions.ToTsVector(
                    "simple",
                    EF.Functions.Unaccent(recipe.Title) + " " +
                    EF.Functions.Unaccent(recipe.Description ?? string.Empty))
                .Matches(EF.Functions.PlainToTsQuery(
                    "simple",
                    EF.Functions.Unaccent(normalizedSearchTerm))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(recipe => recipe.Title)
            .ThenBy(recipe => recipe.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<Recipe>(items, totalCount, pageIndex, pageSize);
    }
}