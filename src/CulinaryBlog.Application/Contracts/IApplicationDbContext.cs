using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Contracts;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeStep> RecipeSteps { get; }
    DbSet<RecipeIngredient> RecipeIngredients { get; }
    DbSet<RecipeImage> RecipeImages { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
