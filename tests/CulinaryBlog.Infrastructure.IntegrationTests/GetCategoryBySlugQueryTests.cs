using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryBySlug;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class GetCategoryBySlugQueryTests
{
    [Fact]
    public async Task DetailFiltersBeforeCountingAndPaginatesOwnDraftsAndPublishedRecipes()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var category = new Category { Name = "Soups", Slug = "soups" };
        var now = DateTime.UtcNow;
        context.Categories.Add(category);
        var published = new Recipe { CategoryId = category.Id, Slug = "public", Status = RecipeStatus.Published };
        var draft = new Recipe { CategoryId = category.Id, Slug = "own", AuthorId = "author", Status = RecipeStatus.Draft };
        context.Recipes.AddRange(published, draft,
            new Recipe { CategoryId = category.Id, Slug = "other", AuthorId = "other", Status = RecipeStatus.Draft },
            new Recipe { CategoryId = category.Id, Slug = "deleted", Status = RecipeStatus.Published, IsDeleted = true });
        await context.SaveChangesAsync();
        published.CreatedAt = now;
        draft.CreatedAt = now.AddDays(-1);
        await context.SaveChangesAsync();

        var author = new GetCategoryBySlugQueryHandler(context, new TestUser("author", true));
        var result = await author.Handle(new GetCategoryBySlugQuery("soups", 2, 1), CancellationToken.None);
        var recipes = result!.Category!.Recipes;
        Assert.Equal(2, recipes.TotalCount);
        Assert.Equal(2, recipes.TotalPages);
        Assert.Equal(2, recipes.Page);
        Assert.Equal("own", Assert.Single(recipes.Items).Slug);
        Assert.True(recipes.Items[0].IsDraft);

        foreach (var user in new[] { new TestUser(null, false), new TestUser("author", false) })
        {
            var reader = new GetCategoryBySlugQueryHandler(context, user);
            var publicResult = await reader.Handle(new GetCategoryBySlugQuery("soups"), CancellationToken.None);
            Assert.Equal(1, publicResult!.Category!.Recipes.TotalCount);
            Assert.Equal("public", Assert.Single(publicResult.Category.Recipes.Items).Slug);
        }
    }

    private sealed record TestUser(string? UserId, bool IsAuthor) : ICurrentUserService
    {
        public bool IsAuthenticated => UserId is not null;
        public bool IsInRole(string role) => IsAuthor && role == "Author";
    }
}
