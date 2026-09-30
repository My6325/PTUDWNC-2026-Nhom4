using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Application.Features.Recipes.PublishRecipe;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Domain.Exceptions;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class PublishRecipeCommandTests
{
    [Fact]
    public async Task PublishRecipe_ShouldPublishAndInvalidateCaches()
    {
        var recipe = CreateEligibleRecipe();
        var repository = new FakeRecipeRepository(recipe);
        var unitOfWork = new FakeUnitOfWork(repository);
        var invalidator = new FakeCacheInvalidator();
        var handler = new PublishRecipeCommandHandler(unitOfWork, new AllowRecipeAuthorization(), invalidator);

        await handler.Handle(new PublishRecipeCommand(recipe.Id), CancellationToken.None);

        Assert.Equal(RecipeStatus.Published, recipe.Status);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(1, invalidator.ListInvalidationCount);
        Assert.Equal(new[] { recipe.Slug }, invalidator.InvalidatedSlugs);
    }

    [Fact]
    public async Task PublishRecipe_ShouldRejectRecipeWithoutStepsOrIngredients()
    {
        var recipe = Recipe.Create("Soup", "soup", null, null, null, "author", 5, 10, 2, RecipeDifficulty.Easy);
        var unitOfWork = new FakeUnitOfWork(new FakeRecipeRepository(recipe));
        var invalidator = new FakeCacheInvalidator();
        var handler = new PublishRecipeCommandHandler(unitOfWork, new AllowRecipeAuthorization(), invalidator);

        await Assert.ThrowsAsync<RecipeNotEligibleForPublishException>(() =>
            handler.Handle(new PublishRecipeCommand(recipe.Id), CancellationToken.None));

        Assert.Equal(RecipeStatus.Draft, recipe.Status);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Equal(0, invalidator.ListInvalidationCount);
    }

    [Fact]
    public async Task PublishRecipe_ShouldRejectNonDraftRecipe()
    {
        var recipe = CreateEligibleRecipe();
        recipe.Status = RecipeStatus.Published;
        var unitOfWork = new FakeUnitOfWork(new FakeRecipeRepository(recipe));
        var handler = new PublishRecipeCommandHandler(unitOfWork, new AllowRecipeAuthorization(), new FakeCacheInvalidator());

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new PublishRecipeCommand(recipe.Id), CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static Recipe CreateEligibleRecipe()
    {
        var recipe = Recipe.Create("Soup", "soup", null, null, null, "author", 5, 10, 2, RecipeDifficulty.Easy);
        recipe.AddStep(RecipeStep.Create(recipe.Id, 1, "Cook", "Cook it", null, null));
        recipe.AddIngredient(RecipeIngredient.Create(recipe.Id, 0, "Water", 1, "cup", null));
        return recipe;
    }

    private sealed class FakeRecipeRepository(Recipe? recipe) : IRecipeRepository
    {
        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Recipe?>(null);
        public Task<Recipe?> GetByIdForPublishingAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(recipe?.Id == id ? recipe : null);
        public Task<RecipeDetailDto?> GetDetailBySlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult<RecipeDetailDto?>(null);
        public Task AddAsync(Recipe recipe, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> SlugExistsAsync(string slug, Guid? excludedRecipeId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<CulinaryBlog.Domain.Common.PaginatedResult<CulinaryBlog.Application.Features.Recipes.Queries.RecipeListDto>> SearchRecipesAsync(
            string? searchTerm, Guid? categoryId, string? difficulty, string sortBy, int pageIndex, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<CulinaryBlog.Domain.Common.PaginatedResult<CulinaryBlog.Application.Features.Recipes.Queries.RecipeListDto>> SearchRecipesAsync(
            string? searchTerm, int pageIndex, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork(FakeRecipeRepository recipes) : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public IRecipeRepository Recipes { get; } = recipes;
        public ICategoryRepository Categories => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class AllowRecipeAuthorization : IRecipeAuthorizationService
    {
        public Task EnsureCanUpdateAsync(Recipe recipe, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCacheInvalidator : IRecipeCacheInvalidator
    {
        public int ListInvalidationCount { get; private set; }
        public List<string> InvalidatedSlugs { get; } = [];
        public Task InvalidateListAsync(CancellationToken cancellationToken)
        {
            ListInvalidationCount++;
            return Task.CompletedTask;
        }
        public Task InvalidateDetailAsync(string slug, CancellationToken cancellationToken)
        {
            InvalidatedSlugs.Add(slug);
            return Task.CompletedTask;
        }
    }
}
