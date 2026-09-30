using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Contracts;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task AddAsync(Category category, CancellationToken cancellationToken);

    Task<int> GetNextOrderIndexAsync(CancellationToken cancellationToken);
}
