using CulinaryBlog.Application.Features.Categories.Common;
using CulinaryBlog.Application.Features.Categories.Queries;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class GetCategoriesQueryTests
{
    [Fact]
    public async Task GetCategories_ReturnsActiveCategoriesInDisplayOrderWithPublishedCounts()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var broth = new Category { Name = "Broth", Slug = "broth", OrderIndex = 1 };
        var rice = new Category { Name = "Rice", Slug = "rice", OrderIndex = 1 };
        var apples = new Category { Name = "Apples", Slug = "apples", OrderIndex = 2 };
        var deleted = new Category { Name = "Deleted", Slug = "deleted", IsDeleted = true };
        context.Categories.AddRange(rice, apples, broth, deleted);
        var published = new Recipe { Title = "Public", Slug = "public", CategoryId = broth.Id, Status = RecipeStatus.Published };
        context.Recipes.AddRange(published,
            new Recipe { Title = "Draft", Slug = "draft", CategoryId = broth.Id, Status = RecipeStatus.Draft },
            new Recipe { Title = "Archived", Slug = "archived", CategoryId = broth.Id, Status = RecipeStatus.Archived },
            new Recipe { Title = "Deleted", Slug = "deleted", CategoryId = broth.Id, Status = RecipeStatus.Published, IsDeleted = true },
            new Recipe { Title = "Other", Slug = "other", CategoryId = apples.Id, Status = RecipeStatus.Published });
        await context.SaveChangesAsync();

        var handler = new GetCategoriesQueryHandler(context, cache);
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.Equal(new[] { "broth", "rice", "apples" }, result.Select(c => c.Slug));
        Assert.Equal(new int?[] { 1, 0, 1 }, result.Select(c => c.RecipeCount));
        Assert.Same(result, await handler.Handle(new GetCategoriesQuery(), CancellationToken.None));

        published.Status = RecipeStatus.Archived;
        await context.SaveChangesAsync();
        cache.Remove(CategoryCacheKeys.All);
        var refreshed = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);
        Assert.Equal(0, refreshed[0].RecipeCount);
    }

    [Fact]
    public async Task DeleteBlockers_IncludeDraftAndSoftDeletedRecipesAndExcludeUnlinkedRecipes()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var draftCategory = Guid.NewGuid();
        var deletedRecipeCategory = Guid.NewGuid();
        context.Recipes.AddRange(
            new Recipe { Slug = "draft", CategoryId = draftCategory, Status = RecipeStatus.Draft },
            new Recipe { Slug = "another-draft", CategoryId = draftCategory, Status = RecipeStatus.Draft },
            new Recipe { Slug = "deleted", CategoryId = deletedRecipeCategory, IsDeleted = true },
            new Recipe { Slug = "unlinked" });
        await context.SaveChangesAsync();

        var result = await new GetCategoryDeleteBlockersHandler(context)
            .Handle(new GetCategoryDeleteBlockers(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(draftCategory, result);
        Assert.Contains(deletedRecipeCategory, result);
    }
}
