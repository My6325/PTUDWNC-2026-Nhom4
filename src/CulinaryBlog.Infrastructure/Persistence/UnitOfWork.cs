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
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("RECIPE_SLUG_CONFLICT", "Slug công thức đã tồn tại.");
        }
    }
}
