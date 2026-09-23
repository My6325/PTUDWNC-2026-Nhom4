namespace CulinaryBlog.Application.Contracts;

public interface IRecipeCacheInvalidator
{
    Task InvalidateListAsync(CancellationToken cancellationToken);

    Task InvalidateDetailAsync(string slug, CancellationToken cancellationToken);
}
