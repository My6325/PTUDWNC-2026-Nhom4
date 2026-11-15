using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Application.Features.Recipes.CreateDraft;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries;
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

    [Fact]
    public async Task UpdateRecipe_WithStepsAndIngredients_SyncsSuccessfully()
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

        var existingStep = RecipeStep.Create(recipe.Id, 1, "Bước cũ", "Mô tả cũ", 5, null);
        recipe.AddStep(existingStep);
        var oldStepToRemove = RecipeStep.Create(recipe.Id, 2, "Bước cần xóa", "Mô tả", 10, null);
        recipe.AddStep(oldStepToRemove);

        var existingIng = RecipeIngredient.Create(recipe.Id, 1, "Cá lóc", 500, "gam", "Tươi ngon");
        recipe.AddIngredient(existingIng);
        var oldIngToRemove = RecipeIngredient.Create(recipe.Id, 2, "Nước màu", 1, "muỗng", null);
        recipe.AddIngredient(oldIngToRemove);

        var unitOfWork = new FakeUnitOfWork(new FakeRecipeRepository { ExistingRecipe = recipe }, new FakeCategoryRepository(true));
        var handler = new UpdateRecipeCommandHandler(
            unitOfWork,
            new AllowRecipeAuthorizationService(),
            new FakeCacheInvalidator());

        var updateSteps = new List<UpdateRecipeStepItemRequest>
        {
            new UpdateRecipeStepItemRequest(existingStep.Id, "Bước cũ đã sửa", "Mô tả mới", 7, null),
            new UpdateRecipeStepItemRequest(null, "Bước mới thêm", "Mô tả bước mới", 15, null)
        };

        var updateIngredients = new List<UpdateRecipeIngredientItemRequest>
        {
            new UpdateRecipeIngredientItemRequest(existingIng.Id, "Cá lóc đồng", 600, "gam", "Cắt khúc"),
            new UpdateRecipeIngredientItemRequest(null, "Tiêu đen", 1, "muỗng", "Xay nhỏ")
        };

        var nutritionReq = new RecipeNutritionRequest(350, 25, 10, 8, 2, 400);

        var request = new UpdateRecipeRequest(
            "Cá kho tộ đặc biệt",
            "Mô tả chi tiết hơn",
            null,
            null,
            15,
            35,
            4,
            RecipeDifficulty.Medium,
            nutritionReq,
            RecipeMappings.ToBytes(10),
            updateSteps,
            updateIngredients);

        var result = await handler.Handle(new UpdateRecipeCommand(recipe.Id, request), CancellationToken.None);

        Assert.Equal("Cá kho tộ đặc biệt", result.Title);
        Assert.Equal(1, unitOfWork.SaveCount);

        // Verify steps
        var activeSteps = recipe.Steps.Where(s => !s.IsDeleted).OrderBy(s => s.StepNumber).ToList();
        Assert.Equal(2, activeSteps.Count);
        Assert.Equal("Bước cũ đã sửa", activeSteps[0].Title);
        Assert.Equal(1, activeSteps[0].StepNumber);
        Assert.Equal("Bước mới thêm", activeSteps[1].Title);
        Assert.Equal(2, activeSteps[1].StepNumber);
        Assert.True(oldStepToRemove.IsDeleted);

        // Verify ingredients
        var activeIngredients = recipe.Ingredients.Where(i => !i.IsDeleted).OrderBy(i => i.OrderIndex).ToList();
        Assert.Equal(2, activeIngredients.Count);
        Assert.Equal("Cá lóc đồng", activeIngredients[0].Name);
        Assert.Equal(600, activeIngredients[0].Quantity);
        Assert.Equal(1, activeIngredients[0].OrderIndex);
        Assert.Equal("Tiêu đen", activeIngredients[1].Name);
        Assert.Equal(2, activeIngredients[1].OrderIndex);
        Assert.True(oldIngToRemove.IsDeleted);

        // Verify nutrition
        Assert.Equal(350, recipe.Nutrition.Calories);
        Assert.Equal(25, recipe.Nutrition.Protein);
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

        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
            string? searchTerm,
            Guid? categoryId,
            string? difficulty,
            string sortBy,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
            string? searchTerm,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Recipe?> GetByIdForPublishingAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingRecipe);

        public Task<RecipeDetailDto?> GetDetailBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult<RecipeDetailDto?>(null);
    }

    private sealed class FakeCategoryRepository(bool exists) : ICategoryRepository
    {
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(exists);

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddAsync(Category category, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Category?>(null);

        public Task<bool> HasRecipesAsync(Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<int> GetNextOrderIndexAsync(CancellationToken cancellationToken) =>
            Task.FromResult(1);

        public Task<Category?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Category?>(null);

        public Task<List<Category>> GetDeletedCategoriesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new List<Category>());
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
