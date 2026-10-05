using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Contracts;

public interface IRecipeAuthorizationService
{
    Task EnsureCanUpdateAsync(Recipe recipe, CancellationToken cancellationToken);

    Task EnsureCanDeleteAsync(Recipe recipe, CancellationToken cancellationToken) => EnsureCanUpdateAsync(recipe, cancellationToken);
}
