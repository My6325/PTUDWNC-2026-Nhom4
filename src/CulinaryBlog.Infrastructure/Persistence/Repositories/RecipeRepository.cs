using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Repositories;

public sealed class RecipeRepository : IRecipeRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RecipeRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Recipes
            .FirstOrDefaultAsync(recipe => recipe.Id == id && !recipe.IsDeleted, cancellationToken);
    }

    public Task<Recipe?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Recipes
            .Include(recipe => recipe.Steps)
            .Include(recipe => recipe.Ingredients)
            .Include(recipe => recipe.Images)
            .FirstOrDefaultAsync(recipe => recipe.Id == id && !recipe.IsDeleted, cancellationToken);
    }

    public Task<Recipe?> GetByIdForPublishingAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Recipes
            .Include(recipe => recipe.Steps)
            .Include(recipe => recipe.Ingredients)
            .Include(recipe => recipe.Images)
            .FirstOrDefaultAsync(recipe => recipe.Id == id && !recipe.IsDeleted, cancellationToken);
    }

    public async Task<RecipeDetailDto?> GetDetailBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var result = await _dbContext.Recipes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(recipe => recipe.Steps)
            .Include(recipe => recipe.Ingredients)
            .Include(recipe => recipe.Images)
            .Include(recipe => recipe.Category)
            .Where(recipe => recipe.Slug == slug && !recipe.IsDeleted)
            .Select(recipe => new
            {
                Recipe = recipe,
                AuthorName = _dbContext.Users.Where(user => user.Id == recipe.AuthorId)
                    .Select(user => user.DisplayName).FirstOrDefault(),
                AuthorAvatar = _dbContext.Users.Where(user => user.Id == recipe.AuthorId)
                    .Select(user => user.AvatarUrl).FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (result is null) return null;

        var recipe = result.Recipe;
        return new RecipeDetailDto(
            recipe.Id,
            recipe.Title,
            recipe.Slug,
            recipe.Description,
            recipe.Instructions,
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.PrepTimeMinutes + recipe.CookTimeMinutes,
            recipe.Servings,
            recipe.Difficulty,
            recipe.Status,
            recipe.PublishedAt ?? recipe.CreatedAt,
            recipe.AuthorId,
            new RecipeAuthorDto(result.AuthorName ?? string.Empty, result.AuthorAvatar),
            recipe.Category is null ? null : new RecipeCategoryDto(recipe.Category.Id, recipe.Category.Name, recipe.Category.Slug),
            recipe.Steps.Where(step => !step.IsDeleted).OrderBy(step => step.StepNumber).Select(step => new RecipeStepDto(
                step.StepNumber, step.Title, step.Description, step.TimerMinutes, step.ImageUrl)).ToArray(),
            recipe.Ingredients.Where(ingredient => !ingredient.IsDeleted).OrderBy(ingredient => ingredient.OrderIndex).Select(ingredient => new RecipeIngredientDto(
                ingredient.Name, ingredient.Quantity, ingredient.Unit, ingredient.Notes, ingredient.OrderIndex)).ToArray(),
            recipe.Images.Where(image => !image.IsDeleted).OrderByDescending(image => image.IsPrimary).ThenBy(image => image.OrderIndex).Select(image => new RecipeImageDto(
                image.OriginalUrl, image.MediumUrl, image.ThumbnailUrl, image.IsPrimary, image.OrderIndex)).ToArray(),
            new RecipeNutritionDto(recipe.Nutrition.Calories, recipe.Nutrition.Protein, recipe.Nutrition.Carbohydrates,
                recipe.Nutrition.Fat, recipe.Nutrition.Fiber, recipe.Nutrition.Sodium));
    }

    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        await _dbContext.Recipes.AddAsync(recipe, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludedRecipeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Recipes.AnyAsync(
            recipe => recipe.Slug == slug &&
                      !recipe.IsDeleted &&
                      (!excludedRecipeId.HasValue || recipe.Id != excludedRecipeId.Value),
            cancellationToken);
    }

    public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
        string? searchTerm,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return SearchRecipesAsync(searchTerm, null, null, null, null, null, null, "newest", pageIndex, pageSize, cancellationToken);
    }

    public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
        string? searchTerm, Guid? categoryId, string? difficulty, string sortBy,
        int pageIndex, int pageSize, CancellationToken cancellationToken) =>
        SearchRecipesAsync(searchTerm, categoryId, difficulty, null, null, null, null, sortBy, pageIndex, pageSize, cancellationToken);

    public async Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
        string? searchTerm,
        Guid? categoryId,
        string? difficulty,
        int? minCookTimeMinutes,
        int? maxCookTimeMinutes,
        int? minServings,
        int? maxServings,
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

        if (minCookTimeMinutes.HasValue) query = query.Where(recipe => recipe.CookTimeMinutes >= minCookTimeMinutes.Value);
        if (maxCookTimeMinutes.HasValue) query = query.Where(recipe => recipe.CookTimeMinutes <= maxCookTimeMinutes.Value);
        if (minServings.HasValue) query = query.Where(recipe => recipe.Servings >= minServings.Value);
        if (maxServings.HasValue) query = query.Where(recipe => recipe.Servings <= maxServings.Value);

        var normalizedSearchTerm = searchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            var textQuery = EF.Functions.PlainToTsQuery("simple", EF.Functions.Unaccent(normalizedSearchTerm));
            query = query.Where(recipe => EF.Property<NpgsqlTypes.NpgsqlTsVector>(recipe, "SearchVector").Matches(textQuery));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var normalizedSort = string.IsNullOrWhiteSpace(sortBy) ? "newest" : sortBy.Trim();
        IOrderedQueryable<Recipe> orderedQuery = normalizedSort.ToLowerInvariant() switch
        {
            "newest" => query.OrderByDescending(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id),
            "cooktime" => query.OrderBy(recipe => recipe.CookTimeMinutes).ThenByDescending(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id),
            "relevance" when !string.IsNullOrWhiteSpace(normalizedSearchTerm) => query
                .OrderByDescending(recipe => EF.Property<NpgsqlTypes.NpgsqlTsVector>(recipe, "SearchVector")
                    .Rank(EF.Functions.PlainToTsQuery("simple", EF.Functions.Unaccent(normalizedSearchTerm))))
                .ThenByDescending(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), "Unsupported recipe sort order.")
        };

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
                        .Where(image => image.IsPrimary && !image.IsDeleted)
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
