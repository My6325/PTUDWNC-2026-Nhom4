using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class RecipeRepositorySupabaseTests
{
    [Fact]
    public async Task SearchRecipesAsync_ShouldMatchVietnameseTitleWithoutDiacritics()
    {
        await using var context = CreateDbContext();
        var repository = new RecipeRepository(context);

        var result = await repository.SearchRecipesAsync("bun bo", 1, 20, CancellationToken.None);

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

    private static ApplicationDbContext CreateDbContext()
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["SUPABASE_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings:DefaultConnection or SUPABASE_CONNECTION_STRING before running Supabase integration tests.");
        }

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
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, ".env")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root containing .env.");
    }
}