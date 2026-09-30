using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Contracts;

/// <summary>
/// Interface trừu tượng hóa DbContext phục vụ tầng Application theo chuẩn Clean Architecture.
/// Giúp các CQRS Handler truy vấn dữ liệu mà không bị phụ thuộc trực tiếp vào tầng Infrastructure.
/// </summary>
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
