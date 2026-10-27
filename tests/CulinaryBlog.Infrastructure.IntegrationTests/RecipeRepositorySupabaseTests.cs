using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Repositories;
using CulinaryBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class RecipeRepositorySupabaseTests
{
    [Theory]
    [InlineData("bun bo", "newest")]
    [InlineData("bun bo", "cookTime")]
    [InlineData("bun bo", "relevance")]
    [InlineData("Bún Bò", "relevance")]
    [InlineData("  bun bo  ", "relevance")]
    public async Task SearchRecipesAsync_ShouldMatchVietnameseTitle(string searchTerm, string sortBy)
    {
        await using var context = CreateDbContext();
        var repository = new RecipeRepository(context);

        var result = await repository.SearchRecipesAsync(searchTerm, null, null, sortBy, 1, 20, CancellationToken.None);

        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, recipe =>
            recipe.Title.Contains("Bún Bò", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchRecipesAsync_ShouldReturnSeededDatasetPage()
    {
        await using var context = CreateDbContext();
        var repository = new RecipeRepository(context);

        var result = await repository.SearchRecipesAsync(null, 1, 20, CancellationToken.None);

        Assert.True(result.TotalCount >= 100);
        Assert.Equal((int)Math.Ceiling(result.TotalCount / 20d), result.TotalPages);
        Assert.Equal(20, result.Items.Count);
        Assert.Equal(1, result.PageIndex);
        Assert.Equal(20, result.PageSize);
    }

    [Theory]
    [InlineData("newest")]
    [InlineData("cookTime")]
    public async Task SearchRecipesAsync_ShouldCombineFacetsAndKeepPageOrder(string sortBy)
    {
        await using var context = CreateDbContext();
        var repository = new RecipeRepository(context);
        var published = await context.Recipes.AsNoTracking()
            .Where(recipe => recipe.Status == RecipeStatus.Published && !recipe.IsDeleted)
            .ToListAsync();
        var sample = published.First(recipe => recipe.CategoryId.HasValue);
        var expected = published.Where(recipe => recipe.CategoryId == sample.CategoryId
            && recipe.Difficulty == sample.Difficulty
            && recipe.CookTimeMinutes >= 0 && recipe.CookTimeMinutes <= sample.CookTimeMinutes
            && recipe.Servings >= 1 && recipe.Servings <= sample.Servings);
        var ordered = sortBy == "newest"
            ? expected.OrderByDescending(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id)
            : expected.OrderBy(recipe => recipe.CookTimeMinutes).ThenByDescending(recipe => recipe.CreatedAt).ThenBy(recipe => recipe.Id);
        var expectedIds = ordered.Select(recipe => recipe.Id).ToArray();
        Assert.NotEmpty(expectedIds);

        for (var page = 1; page <= Math.Min(expectedIds.Length + 1, 3); page++)
        {
            var result = await repository.SearchRecipesAsync(null, sample.CategoryId,
                sample.Difficulty.ToString(), 0, sample.CookTimeMinutes, 1, sample.Servings,
                sortBy, page, 1, CancellationToken.None);
            Assert.Equal(expectedIds.Length, result.TotalCount);
            Assert.Equal(expectedIds.Skip(page - 1).Take(1), result.Items.Select(recipe => recipe.Id));
        }
    }

    [Fact]
    public async Task GetDetailBySlugAsync_ShouldLoadAllPublicRecipeSections()
    {
        await using var context = CreateDbContext();
        var sample = await context.Recipes.AsNoTracking()
            .Include(recipe => recipe.Steps).Include(recipe => recipe.Ingredients)
            .Include(recipe => recipe.Images).Include(recipe => recipe.Category)
            .FirstAsync(recipe => recipe.Status == RecipeStatus.Published && !recipe.IsDeleted
                && recipe.Steps.Any() && recipe.Ingredients.Any() && recipe.Images.Any());
        var result = await new RecipeRepository(context).GetDetailBySlugAsync(sample.Slug, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sample.Steps.Where(step => !step.IsDeleted).OrderBy(step => step.StepNumber)
            .Select(step => step.Description), result.Steps.Select(step => step.Description));
        Assert.Equal(sample.Ingredients.Where(item => !item.IsDeleted).OrderBy(item => item.OrderIndex)
            .Select(item => item.Name), result.Ingredients.Select(item => item.Name));
        Assert.Equal(sample.Images.Count(item => !item.IsDeleted), result.Images.Count);
        Assert.Equal(sample.Nutrition.Calories, result.Nutrition.Calories);
        Assert.Equal(sample.Category?.Name, result.Category?.Name);
    }

    [Fact]
    public async Task AddImageToTrackedRecipe_ShouldPersistSuccessfullyWithoutConcurrencyException()
    {
        await using var context = CreateDbContext();

        var recipe = await context.Recipes
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => !r.IsDeleted);

        Assert.NotNull(recipe);

        var newImage = new Domain.Entities.RecipeImage
        {
            RecipeId = recipe.Id,
            OriginalUrl = $"https://test.local/{Guid.NewGuid()}.png",
            IsPrimary = false,
            OrderIndex = recipe.Images.Count + 1
        };

        recipe.AddImage(newImage);

        // Lưu thay đổi - không được văng DbUpdateConcurrencyException
        var affected = await context.SaveChangesAsync();
        Assert.True(affected > 0);

        // Dọn dẹp test data
        context.RecipeImages.Remove(newImage);
        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var configuration = BuildConfiguration();
        var connectionString = SupabaseConnectionStringResolver.Resolve(configuration);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var repositoryRoot = FindRepositoryRoot();
        var builder = new ConfigurationBuilder()
            .SetBasePath(repositoryRoot)
            .AddJsonFile("src/CulinaryBlog.API/appsettings.json", optional: true)
            .AddJsonFile("src/CulinaryBlog.API/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables();

        var envPath = Path.Combine(repositoryRoot, ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex > 0)
                {
                    builder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [trimmed[..separatorIndex].Trim()] = trimmed[(separatorIndex + 1)..].Trim()
                    });
                }
            }
        }

        return builder.Build();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src", "CulinaryBlog.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root containing src/CulinaryBlog.slnx.");
    }
}
