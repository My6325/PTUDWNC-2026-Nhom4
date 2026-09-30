using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class UnitOfWork(
    ApplicationDbContext dbContext,
    IRecipeRepository recipes,
    ICategoryRepository categories) : IUnitOfWork
{
    public IRecipeRepository Recipes { get; } = recipes;

    public ICategoryRepository Categories { get; } = categories;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RecipeConcurrencyConflictException();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            if (postgresException.ConstraintName?.Contains("Categories_Slug", StringComparison.OrdinalIgnoreCase) == true ||
                postgresException.ConstraintName?.Contains("IX_Categories_Slug", StringComparison.OrdinalIgnoreCase) == true)
            {
                throw new ConflictException("CATEGORY_SLUG_CONFLICT", "Slug danh mục đã tồn tại.");
            }

            throw new ConflictException("RECIPE_SLUG_CONFLICT", "Recipe slug already exists.");
        }
    }
}
