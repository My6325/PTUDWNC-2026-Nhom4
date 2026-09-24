namespace CulinaryBlog.Application.Contracts;

public interface ICategoryRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}
