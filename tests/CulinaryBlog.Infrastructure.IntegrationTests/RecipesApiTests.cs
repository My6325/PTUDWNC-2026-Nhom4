using System.Net;
using System.Text.Json;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class RecipesApiTests
{
    [Fact]
    public async Task GetRecipes_ShouldReturnFilteredPaginatedDtoFromTemporaryBackend()
    {
        await using var backend = await TemporaryRecipeBackend.StartAsync();

        var response = await backend.Client.GetAsync(
            $"/api/v1/recipes?CategoryId={TemporaryRecipeBackend.MainCategoryId}&Difficulty=Easy&PageIndex=1&PageSize=10");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK but got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var items = root.GetProperty("items");

        Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("Vegetable Soup", items[0].GetProperty("title").GetString());
        Assert.False(items[0].TryGetProperty("steps", out _));
        Assert.False(items[0].TryGetProperty("ingredients", out _));
    }

    [Fact]
    public async Task GetRecipes_ShouldReturnBadRequestForInvalidPageSize()
    {
        await using var backend = await TemporaryRecipeBackend.StartAsync();

        var response = await backend.Client.GetAsync("/api/v1/recipes?PageSize=51");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRecipes_ShouldCacheSameQueryAndVaryByPage()
    {
        await using var backend = await TemporaryRecipeBackend.StartAsync();

        using var firstPageResponse = await backend.Client.GetAsync("/api/v1/recipes?PageIndex=1&PageSize=1");
        using var repeatedFirstPageResponse = await backend.Client.GetAsync("/api/v1/recipes?PageIndex=1&PageSize=1");
        using var secondPageResponse = await backend.Client.GetAsync("/api/v1/recipes?PageIndex=2&PageSize=1");

        Assert.True(firstPageResponse.StatusCode == HttpStatusCode.OK,
            $"First page failed: {await firstPageResponse.Content.ReadAsStringAsync()}");
        Assert.True(repeatedFirstPageResponse.StatusCode == HttpStatusCode.OK,
            $"Repeated first page failed: {await repeatedFirstPageResponse.Content.ReadAsStringAsync()}");
        Assert.True(secondPageResponse.StatusCode == HttpStatusCode.OK,
            $"Second page failed: {await secondPageResponse.Content.ReadAsStringAsync()}");
        Assert.Equal(2, backend.Repository.QueryCount);

        using var firstPage = JsonDocument.Parse(await firstPageResponse.Content.ReadAsStringAsync());
        using var secondPage = JsonDocument.Parse(await secondPageResponse.Content.ReadAsStringAsync());
        Assert.NotEqual(
            firstPage.RootElement.GetProperty("items")[0].GetProperty("title").GetString(),
            secondPage.RootElement.GetProperty("items")[0].GetProperty("title").GetString());
    }

    private sealed class TemporaryRecipeBackend : IAsyncDisposable
    {
        public static readonly Guid MainCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");

        private readonly WebApplication _app;

        private TemporaryRecipeBackend(WebApplication app, HttpClient client, FakeRecipeRepository repository)
        {
            _app = app;
            Client = client;
            Repository = repository;
        }

        public HttpClient Client { get; }

        public FakeRecipeRepository Repository { get; }

        public static async Task<TemporaryRecipeBackend> StartAsync()
        {
            var repository = new FakeRecipeRepository();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(RecipeEndpoints).Assembly.FullName,
                EnvironmentName = "Testing"
            });

            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Services.AddApplication();
            builder.Services.AddSingleton<IRecipeRepository>(repository);
            builder.Services.AddOutputCache(options =>
            {
                options.AddPolicy("RecipesCache", policy => policy
                    .Expire(TimeSpan.FromMinutes(15))
                    .SetVaryByQuery("*")
                    .Tag("recipes"));
                options.AddPolicy("RecipeDetail", policy => policy
                    .Expire(TimeSpan.FromMinutes(5))
                    .SetVaryByRouteValue("slug"));
            });

            var app = builder.Build();
            app.UseOutputCache();
            app.MapRecipeEndpoints();
            await app.StartAsync();

            var address = app.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .Single();

            return new TemporaryRecipeBackend(app, new HttpClient { BaseAddress = new Uri(address) }, repository);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        private readonly RecipeListDto[] _recipes =
        [
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Title = "Vegetable Soup",
                Slug = "vegetable-soup",
                CategoryName = "Soup",
                AuthorName = "Test Author",
                Difficulty = RecipeDifficulty.Easy,
                CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                Title = "Roasted Vegetables",
                Slug = "roasted-vegetables",
                CategoryName = "Vegetables",
                AuthorName = "Test Author",
                Difficulty = RecipeDifficulty.Medium,
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                Title = "Fruit Salad",
                Slug = "fruit-salad",
                CategoryName = "Dessert",
                AuthorName = "Test Author",
                Difficulty = RecipeDifficulty.Easy,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        ];

        private int _queryCount;

        public int QueryCount => Volatile.Read(ref _queryCount);

        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Recipe?>(null);
        public Task<Recipe?> GetByIdForPublishingAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Recipe?>(null);
        public Task<RecipeDetailDto?> GetDetailBySlugAsync(string slug, CancellationToken cancellationToken) => Task.FromResult<RecipeDetailDto?>(null);
        public Task AddAsync(Recipe recipe, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> SlugExistsAsync(string slug, Guid? excludedRecipeId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
            string? searchTerm, int pageIndex, int pageSize, CancellationToken cancellationToken) =>
            SearchRecipesAsync(searchTerm, null, null, "newest", pageIndex, pageSize, cancellationToken);

        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(
            string? searchTerm,
            Guid? categoryId,
            string? difficulty,
            string sortBy,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _queryCount);

            IEnumerable<RecipeListDto> query = _recipes;
            if (categoryId == TemporaryRecipeBackend.MainCategoryId)
            {
                query = query.Where(recipe => recipe.CategoryName == "Soup");
            }

            if (Enum.TryParse<RecipeDifficulty>(difficulty, ignoreCase: true, out var parsedDifficulty))
            {
                query = query.Where(recipe => recipe.Difficulty == parsedDifficulty);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(recipe => recipe.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            var filteredRecipes = query
                .OrderByDescending(recipe => recipe.CreatedAt)
                .ThenBy(recipe => recipe.Id)
                .ToArray();
            var items = filteredRecipes
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult(new PaginatedResult<RecipeListDto>(
                items,
                filteredRecipes.Length,
                pageIndex,
                pageSize));
        }
    }
}
