using CulinaryBlog.Application;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class RecipeQueryPipelineTests
{
    [Fact]
    public async Task Send_ShouldRejectPageSizeAboveSrsMaximum()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IRecipeRepository, FakeRecipeRepository>();
        using var provider = services.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            sender.Send(new GetRecipesQuery { PageSize = 51 }));
    }

    [Fact]
    public async Task Send_ShouldPassValidQueryThroughHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IRecipeRepository, FakeRecipeRepository>();
        using var provider = services.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();
        var result = await sender.Send(new GetRecipesQuery { PageIndex = 2, PageSize = 25 });

        Assert.Equal(2, result.PageIndex);
        Assert.Equal(25, result.PageSize);
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
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
            return Task.FromResult(new PaginatedResult<RecipeListDto>(
                Array.Empty<RecipeListDto>(),
                totalCount: 0,
                pageIndex,
                pageSize));
        }
    }
}
