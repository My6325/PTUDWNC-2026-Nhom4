using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class RecipeMigrationTests
{
    [Fact]
    public void RepairMigration_ShouldBeDiscoveredAndGenerateSqlWithoutConnecting()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options;
        using var context = new ApplicationDbContext(options);
        const string repair = "20260922091500_RepairRecipeTables";
        Assert.Contains(repair, context.Database.GetMigrations());
        var migrator = context.GetService<IMigrator>();
        var script = migrator.GenerateScript("20260921144807_InitialCreate", repair);
        Assert.Contains("CREATE TABLE IF NOT EXISTS \"Recipes\"", script);
        Assert.Contains("CREATE TABLE IF NOT EXISTS \"RecipeSteps\"", script);
        var rollback = migrator.GenerateScript(repair, "20260921144807_InitialCreate");
        Assert.DoesNotContain("DROP TABLE", rollback, StringComparison.OrdinalIgnoreCase);
    }
}
