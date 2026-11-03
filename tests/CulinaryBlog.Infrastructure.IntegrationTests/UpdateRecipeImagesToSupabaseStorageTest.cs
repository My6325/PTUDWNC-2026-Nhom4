using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class UpdateRecipeImagesToSupabaseStorageTest
{
    private readonly ITestOutputHelper _output;

    public UpdateRecipeImagesToSupabaseStorageTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task UpdateAllRecipeImages_ToSupabaseStorage_ShouldSucceed()
    {
        await using var context = CreateDbContext();

        var sqlPath = Path.Combine(FindRepositoryRoot(), "scripts", "update_recipe_images_supabase.sql");
        var sql = await File.ReadAllTextAsync(sqlPath);

        // Find the end of the UPDATE statement
        var endOfUpdate = "AND ri.\"IsDeleted\" = FALSE;";
        var index = sql.IndexOf(endOfUpdate, StringComparison.Ordinal);
        var updateSql = index > 0 ? sql[..(index + endOfUpdate.Length)] : sql;
        var rowsAffected = await context.Database.ExecuteSqlRawAsync(updateSql);

        _output.WriteLine($"Successfully updated {rowsAffected} recipe image rows.");
        Assert.True(rowsAffected > 0, "At least some recipe images should be updated.");

        // Verify that all active recipe images are now pointing to Supabase Storage
        var recipesWithImages = await context.Recipes
            .Where(r => !r.IsDeleted)
            .Select(r => new
            {
                r.Title,
                PrimaryImage = r.Images.FirstOrDefault(i => !i.IsDeleted && i.IsPrimary)
            })
            .OrderBy(r => r.Title)
            .Take(30)
            .ToListAsync();

        foreach (var item in recipesWithImages)
        {
            var url = item.PrimaryImage?.OriginalUrl ?? "NO_IMAGE";
            _output.WriteLine($"[VERIFIED] {item.Title,-35} => {url}");
            Assert.Contains("supabase.co/storage/v1/object/public/culinary-blog", url);
        }
    }

    private static ApplicationDbContext CreateDbContext()
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
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#')) continue;

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

        var configuration = builder.Build();
        var connectionString = SupabaseConnectionStringResolver.Resolve(configuration);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src", "CulinaryBlog.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
