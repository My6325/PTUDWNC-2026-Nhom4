using CulinaryBlog.Application;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Categories.Common;
using CulinaryBlog.Application.Features.Categories.Create;
using CulinaryBlog.Application.Features.Categories.Delete;
using CulinaryBlog.Application.Features.Categories.Update;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class CreateCategoryCommandTests
{
    [Fact]
    public async Task CreateCategory_ShouldGenerateVietnameseSlugAndInvalidateListCache()
    {
        using var provider = CreateServices();
        var cache = provider.GetRequiredService<IMemoryCache>();
        cache.Set(CategoryCacheKeys.All, new object());

        var result = await provider.GetRequiredService<ISender>().Send(
            new CreateCategoryCommand(new CreateCategoryRequest("Món Khai Vị", "Các món ăn nhẹ")));

        Assert.Equal("mon-khai-vi", result.Slug);
        Assert.Equal("Các món ăn nhẹ", result.Description);
        Assert.False(cache.TryGetValue(CategoryCacheKeys.All, out _));
    }

    [Fact]
    public async Task CreateCategory_ShouldAddNumericSuffixWhenSlugExists()
    {
        using var provider = CreateServices();
        var sender = provider.GetRequiredService<ISender>();
        var first = await sender.Send(new CreateCategoryCommand(new CreateCategoryRequest("Món ăn vặt", null)));

        var result = await sender.Send(new CreateCategoryCommand(new CreateCategoryRequest("Mon an vat", null)));

        Assert.Equal("mon-an-vat", first.Slug);
        Assert.Equal("mon-an-vat-2", result.Slug);
        Assert.Null(result.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("<script>alert(1)</script>")]
    public async Task CreateCategory_ShouldRejectInvalidName(string name)
    {
        using var provider = CreateServices();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            provider.GetRequiredService<ISender>().Send(
                new CreateCategoryCommand(new CreateCategoryRequest(name, null))));

        Assert.Empty(provider.GetRequiredService<FakeCategoryRepository>().Categories);
    }

    [Fact]
    public async Task CreateCategory_ShouldRejectDuplicateNameEvenWhenSlugSuffixIsAvailable()
    {
        using var provider = CreateServices();
        var sender = provider.GetRequiredService<ISender>();
        await sender.Send(new CreateCategoryCommand(new CreateCategoryRequest("Món Chính", null)));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            sender.Send(new CreateCategoryCommand(new CreateCategoryRequest("  MÓN CHÍNH  ", null))));

        Assert.Equal("CATEGORY_NAME_CONFLICT", error.Code);
        Assert.Single(provider.GetRequiredService<FakeCategoryRepository>().Categories);
    }

    [Fact]
    public async Task UpdateCategory_ShouldRetainSlugAndAllowDescriptionOnly()
    {
        using var provider = CreateServices();
        var sender = provider.GetRequiredService<ISender>();
        var created = await sender.Send(new CreateCategoryCommand(new CreateCategoryRequest("Món Chính", "Mô tả cũ", "image")));
        var renamed = await sender.Send(new UpdateCategoryCommand(created.Id, new UpdateCategoryRequest(Name: "Thực đơn")));
        var updated = await sender.Send(new UpdateCategoryCommand(created.Id, new UpdateCategoryRequest(Description: "Mô tả mới")));

        Assert.Equal(created.Slug, renamed.Slug);
        Assert.Equal(created.Slug, updated.Slug);
        Assert.Equal("Thực đơn", updated.Name);
        Assert.Equal("Mô tả mới", updated.Description);
        Assert.Equal(created.ImageUrl, updated.ImageUrl);
        Assert.Equal(created.OrderIndex, updated.OrderIndex);
    }

    [Fact]
    public async Task DeleteCategory_ShouldBlockLinkedRecipesAndSoftDeleteWhenEmpty()
    {
        using var provider = CreateServices();
        var sender = provider.GetRequiredService<ISender>();
        var repository = provider.GetRequiredService<FakeCategoryRepository>();
        var created = await sender.Send(new CreateCategoryCommand(new CreateCategoryRequest("Món Chính", null)));
        repository.LinkedRecipeCount = 2;
        var error = await Assert.ThrowsAsync<ConflictException>(() => sender.Send(new DeleteCategoryCommand(created.Id)));
        Assert.Equal("CATEGORY_HAS_RECIPES", error.Code);
        Assert.Contains("2 công thức", error.Message);
        var category = Assert.Single(repository.Categories);
        Assert.False(category.IsDeleted);

        repository.LinkedRecipeCount = 0;
        await sender.Send(new DeleteCategoryCommand(created.Id));
        Assert.True(category.IsDeleted);
        Assert.NotNull(category.DeletedAt);
    }

    private static ServiceProvider CreateServices(params string[] existingSlugs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddApplication();
        var categoryRepository = new FakeCategoryRepository(existingSlugs);
        services.AddSingleton(categoryRepository);
        services.AddSingleton<ICategoryRepository>(categoryRepository);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork(categoryRepository));
        services.AddSingleton<IRecipeRepository, FakeRecipeRepository>();
        return services.BuildServiceProvider();
    }

    private sealed class FakeCategoryRepository(params string[] existingSlugs) : ICategoryRepository
    {
        private readonly HashSet<string> _slugs = new(existingSlugs, StringComparer.OrdinalIgnoreCase);
        public List<Category> Categories { get; } = [];
        public int LinkedRecipeCount { get; set; }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) => Task.FromResult(_slugs.Contains(slug));
        public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken, Guid? excludedId = null) =>
            Task.FromResult(Categories.Any(category => category.Id != excludedId &&
                string.Equals(category.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            Categories.Add(category);
            _slugs.Add(category.Slug);
            return Task.CompletedTask;
        }
        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Categories.FirstOrDefault(c => c.Id == id));


        public Task<int> CountRecipesAsync(Guid categoryId, CancellationToken cancellationToken) => Task.FromResult(LinkedRecipeCount);
        public Task<int> GetNextOrderIndexAsync(CancellationToken cancellationToken) => Task.FromResult(Categories.Count);
        public Task<Category?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Categories.FirstOrDefault(category => category.Id == id && category.IsDeleted));
        public Task<List<Category>> GetDeletedCategoriesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Categories.Where(category => category.IsDeleted).ToList());
    }

    private sealed class FakeUnitOfWork(ICategoryRepository categories) : IUnitOfWork
    {
        public IRecipeRepository Recipes => throw new NotSupportedException();
        public ICategoryRepository Categories { get; } = categories;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Recipe?>(null);
        public Task<Recipe?> GetByIdForPublishingAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Recipe?>(null);
        public Task<RecipeDetailDto?> GetDetailBySlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult<RecipeDetailDto?>(null);
        public Task AddAsync(Recipe recipe, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> SlugExistsAsync(string slug, Guid? excludedRecipeId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(string? searchTerm, int pageIndex, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(string? searchTerm, Guid? categoryId, string? difficulty, string sortBy, int pageIndex, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
