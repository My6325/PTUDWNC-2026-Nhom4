using System.Data;
using System.Data.Common;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class RecipeSearchSqlTests
{
    [Theory]
    [InlineData("bun bo", "newest")]
    [InlineData("Bún Bò", "relevance")]
    [InlineData("  bun bo  ", "cookTime")]
    public async Task Search_ShouldTranslateFtsFiltersSortAndProjection(string term, string sort)
    {
        var capture = new CaptureCommands();
        await using var context = CreateContext(capture);
        var repository = new RecipeRepository(context);

        await Assert.ThrowsAsync<QueryCapturedException>(() => repository.SearchRecipesAsync(
            term, Guid.NewGuid(), "Easy", 5, 60, 1, 4, sort, 2, 10, CancellationToken.None));

        Assert.Equal(2, capture.Sql.Count);
        foreach (var sql in capture.Sql)
        {
            Assert.Contains("@@ plainto_tsquery('simple', unaccent(", sql);
            Assert.Contains("\"SearchVector\"", sql);
            Assert.Contains("\"CategoryId\"", sql);
            Assert.Contains("\"Difficulty\"", sql);
            Assert.Contains("\"CookTimeMinutes\" >=", sql);
            Assert.Contains("\"CookTimeMinutes\" <=", sql);
            Assert.Contains("\"Servings\" >=", sql);
            Assert.Contains("\"Servings\" <=", sql);
            Assert.Contains(term.Trim(), capture.ParameterValues);
        }

        var itemsSql = capture.Sql[1];
        var orderClause = itemsSql[itemsSql.IndexOf("ORDER BY", StringComparison.Ordinal)..];
        Assert.Contains(sort switch
        {
            "relevance" => "ORDER BY ts_rank(",
            "cookTime" => "ORDER BY r.\"CookTimeMinutes\"",
            _ => "ORDER BY r.\"CreatedAt\" DESC"
        }, orderClause);
        Assert.Contains("LIMIT", itemsSql);
        Assert.Contains("OFFSET", itemsSql);
        Assert.DoesNotContain("Nutrition_", itemsSql);
        Assert.DoesNotContain("RecipeSteps", itemsSql);
        Assert.DoesNotContain("RecipeIngredients", itemsSql);
    }

    [Fact]
    public async Task MissingSearchColumn_ShouldNotSilentlyReturnDifferentSearchResults()
    {
        var failure = new PostgresException("SearchVector is missing", "ERROR", "ERROR", "42703");
        var capture = new CaptureCommands { Failure = failure };
        await using var context = CreateContext(capture);
        var repository = new RecipeRepository(context);

        var actual = await Assert.ThrowsAsync<PostgresException>(() =>
            repository.SearchRecipesAsync("bun bo", 1, 10, CancellationToken.None));

        Assert.Same(failure, actual);
        Assert.Single(capture.Sql);
    }

    [Theory]
    [InlineData(42949673, 50, true)]
    [InlineData(42949674, 50, false)]
    [InlineData(int.MaxValue, 50, false)]
    [InlineData(int.MaxValue, 1, true)]
    [InlineData(1, 51, false)]
    public async Task Pagination_ShouldValidateOffsetBeforeExecutingSql(int pageIndex, int pageSize, bool valid)
    {
        var validation = new GetRecipesQueryValidator().Validate(new GetRecipesQuery
        {
            PageIndex = pageIndex, PageSize = pageSize
        });
        Assert.Equal(valid, validation.IsValid);

        if (!valid)
        {
            var capture = new CaptureCommands();
            await using var context = CreateContext(capture);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                new RecipeRepository(context).SearchRecipesAsync(null, pageIndex, pageSize, CancellationToken.None));
            Assert.Empty(capture.Sql);
        }
    }

    private static ApplicationDbContext CreateContext(CaptureCommands capture) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .AddInterceptors(new SuppressConnection(), capture).Options);

    // Exercise the real Npgsql translator without connecting to or modifying a database.
    private sealed class SuppressConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default) => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class QueryCapturedException : Exception;

    private sealed class CaptureCommands : DbCommandInterceptor
    {
        public List<string> Sql { get; } = [];
        public List<object?> ParameterValues { get; } = [];
        public Exception? Failure { get; init; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            ParameterValues.AddRange(command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value));
            if (Failure is not null) throw Failure;
            if (Sql.Count > 1) throw new QueryCapturedException();

            var count = new DataTable();
            count.Columns.Add("count", typeof(int));
            count.Rows.Add(1);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(count.CreateDataReader()));
        }
    }
}
