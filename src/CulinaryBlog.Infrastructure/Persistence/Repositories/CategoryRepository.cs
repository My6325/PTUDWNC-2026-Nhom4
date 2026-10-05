using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(ApplicationDbContext dbContext) : ICategoryRepository
{
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Categories.AnyAsync(
            category => category.Id == id && !category.IsDeleted,
            cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
    {
        // Include soft-deleted rows because the database unique index also includes them.
        return dbContext.Categories.AnyAsync(category => category.Slug == slug, cancellationToken);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        return dbContext.Categories.AddAsync(category, cancellationToken).AsTask();
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Categories.FirstOrDefaultAsync(
            category => category.Id == id && !category.IsDeleted,
            cancellationToken);
    }

    public Task<bool> HasRecipesAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return dbContext.Recipes.AnyAsync(
            recipe => recipe.CategoryId == categoryId && !recipe.IsDeleted,
            cancellationToken);
    }

    public async Task<int> GetNextOrderIndexAsync(CancellationToken cancellationToken)
    {
        var maxOrderIndex = await dbContext.Categories.Select(category => (int?)category.OrderIndex)
            .MaxAsync(cancellationToken);
        return maxOrderIndex.GetValueOrDefault(-1) + 1;
    }
}
