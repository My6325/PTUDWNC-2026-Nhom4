using CulinaryBlog.Application.Contracts;
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
}
