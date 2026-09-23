using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Application.Features.Recipes.CreateDraft;
using CulinaryBlog.Application.Features.Recipes.Update;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Xunit;

namespace CulinaryBlog.Application.UnitTests;

public sealed class RecipeCommandHandlerTests
{
    [Fact]
    public async Task CreateDraft_AssignsCurrentAuthorAndDraftStatus()
    {
        var recipes = new FakeRecipeRepository();
        var unitOfWork = new FakeUnitOfWork(recipes, new FakeCategoryRepository(true));
        var handler = new CreateRecipeDraftCommandHandler(
            unitOfWork,
            new FakeCurrentUserService("author-01"),
            new FakeCacheInvalidator());

        var result = await handler.Handle(
            new CreateRecipeDraftCommand(CreateRequest()),
            CancellationToken.None);

        Assert.Equal("author-01", result.AuthorId);
        Assert.Equal(RecipeStatus.Draft, result.Status);
        Assert.Equal("ca-kho-to", result.Slug);
        Assert.NotNull(recipes.AddedRecipe);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateDraft_DuplicateSlugThrowsConflict()
    {
        var recipes = new FakeRecipeRepository { SlugExists = true };
        var handler = new CreateRecipeDraftCommandHandler(
            new FakeUnitOfWork(recipes, new FakeCategoryRepository(true)),
            new FakeCurrentUserService("author-01"),
            new FakeCacheInvalidator());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateRecipeDraftCommand(CreateRequest()), CancellationToken.None));

        Assert.Equal("RECIPE_SLUG_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task CreateDraft_InvalidCategoryThrowsUnprocessableEntity()
    {
        var handler = new CreateRecipeDraftCommandHandler(
            new FakeUnitOfWork(new FakeRecipeRepository(), new FakeCategoryRepository(false)),
            new FakeCurrentUserService("author-01"),
            new FakeCacheInvalidator());

        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(() =>
            handler.Handle(new CreateRecipeDraftCommand(CreateRequest(Guid.NewGuid())), CancellationToken.None));

        Assert.Equal("RECIPE_CATEGORY_INVALID", exception.Code);
    }

    [Fact]
    public async Task Update_StaleRowVersionThrowsConcurrencyConflict()
    {
        var recipe = Recipe.Create(
            "Cá kho tộ",
            "ca-kho-to",
            "Mô tả",
            null,
            null,
            "author-01",
            10,
            20,
            2,
            RecipeDifficulty.Easy);
        recipe.RowVersion = 10;

        var recipes = new FakeRecipeRepository { ExistingRecipe = recipe };
        var handler = new UpdateRecipeCommandHandler(
            new FakeUnitOfWork(recipes, new FakeCategoryRepository(true)),
            new AllowRecipeAuthorizationService(),
            new FakeCacheInvalidator());
        var request = new UpdateRecipeRequest(
            "Cá kho mới",
            "Mô tả mới",
            null,
            null,
            15,
            30,
            4,
            RecipeDifficulty.Medium,
            null,
            RecipeMappings.ToBytes(9));

        await Assert.ThrowsAsync<RecipeConcurrencyConflictException>(() =>
            handler.Handle(new UpdateRecipeCommand(recipe.Id, request), CancellationToken.None));
    }

    private static CreateRecipeDraftRequest CreateRequest(Guid? categoryId = null)
    {
        return new CreateRecipeDraftRequest(
            "Cá kho tộ",
            "Mô tả",
            categoryId,
            10,
            20,
            2,
            RecipeDifficulty.Easy,
            null,
            null,
            null,
            null);
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        public Recipe? ExistingRecipe { get; init; }
        public Recipe? AddedRecipe { get; private set; }
        public bool SlugExists { get; init; }

        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingRecipe);

        public Task AddAsync(Recipe recipe, CancellationToken cancellationToken)
        {
            AddedRecipe = recipe;
            return Task.CompletedTask;
        }

        public Task<bool> SlugExistsAsync(
            string slug,
            Guid? excludedRecipeId,
            CancellationToken cancellationToken) => Task.FromResult(SlugExists);

        public Task<PaginatedResult<Recipe>> SearchRecipesAsync(
            string? searchTerm,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCategoryRepository(bool exists) : ICategoryRepository
    {
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(exists);
    }

    private sealed class FakeUnitOfWork(
        IRecipeRepository recipes,
        ICategoryRepository categories) : IUnitOfWork
    {
        public IRecipeRepository Recipes { get; } = recipes;
        public ICategoryRepository Categories { get; } = categories;
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCurrentUserService(string userId) : ICurrentUserService
    {
        public string? UserId { get; } = userId;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => role == "Author";
    }

    private sealed class FakeCacheInvalidator : IRecipeCacheInvalidator
    {
        public Task InvalidateListAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task InvalidateDetailAsync(string slug, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class AllowRecipeAuthorizationService : IRecipeAuthorizationService
    {
        public Task EnsureCanUpdateAsync(Recipe recipe, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
