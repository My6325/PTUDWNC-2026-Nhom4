using CulinaryBlog.Application;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Features.Categories.Common;
using CulinaryBlog.Application.Features.Categories.Create;
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
        using var provider = CreateServices("mon-chinh");

        var result = await provider.GetRequiredService<ISender>().Send(
            new CreateCategoryCommand(new CreateCategoryRequest("Món Chính", null)));

        Assert.Equal("mon-chinh-2", result.Slug);
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

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) => Task.FromResult(_slugs.Contains(slug));
        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            Categories.Add(category);
            _slugs.Add(category.Slug);
            return Task.CompletedTask;
        }
        public Task<int> GetNextOrderIndexAsync(CancellationToken cancellationToken) => Task.FromResult(Categories.Count);
        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Categories.FirstOrDefault(c => c.Id == id));
        public Task<bool> HasRecipesAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(false);
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
