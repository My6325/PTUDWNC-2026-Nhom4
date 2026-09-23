using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Enums;
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

    public async Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
        string? searchTerm,
        Guid? categoryId,
        string? difficulty,
        string sortBy,
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
            .Where(recipe => recipe.Status == RecipeStatus.Published && !recipe.IsDeleted)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(recipe => recipe.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(difficulty)
            && Enum.TryParse<RecipeDifficulty>(difficulty, ignoreCase: true, out var parsedDifficulty))
        {
            query = query.Where(recipe => recipe.Difficulty == parsedDifficulty);
        }

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

        var orderedQuery = sortBy.Equals("newest", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id)
            : throw new ArgumentOutOfRangeException(nameof(sortBy), "Only the 'newest' sort is currently supported.");

        var items = await (
                from recipe in orderedQuery
                join author in _dbContext.Users.AsNoTracking()
                    on recipe.AuthorId equals author.Id into authors
                from author in authors.DefaultIfEmpty()
                select new RecipeListDto
                {
                    Id = recipe.Id,
                    Title = recipe.Title,
                    Slug = recipe.Slug,
                    CoverImageUrl = recipe.Images
                        .Where(image => image.IsPrimary)
                        .OrderBy(image => image.OrderIndex)
                        .ThenBy(image => image.Id)
                        .Select(image => image.MediumUrl ?? image.OriginalUrl)
                        .FirstOrDefault(),
                    CategoryName = recipe.Category == null ? null : recipe.Category.Name,
                    AuthorName = author == null ? string.Empty : author.DisplayName,
                    Difficulty = recipe.Difficulty,
                    CreatedAt = recipe.CreatedAt
                })
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<RecipeListDto>(items, totalCount, pageIndex, pageSize);
    }
}
