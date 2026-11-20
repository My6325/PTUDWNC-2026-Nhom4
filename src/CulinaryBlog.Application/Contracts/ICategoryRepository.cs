using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Contracts;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken, Guid? excludedId = null);

    Task AddAsync(Category category, CancellationToken cancellationToken);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountRecipesAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<int> GetNextOrderIndexAsync(CancellationToken cancellationToken);

    Task<Category?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<Category>> GetDeletedCategoriesAsync(CancellationToken cancellationToken);
}
