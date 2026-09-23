namespace CulinaryBlog.Application.Contracts;

public interface IUnitOfWork
{
    IRecipeRepository Recipes { get; }

    ICategoryRepository Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
