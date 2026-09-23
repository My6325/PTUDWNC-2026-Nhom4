using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>
/// DbContext trung tâm của ứng dụng Culinary Blog kết nối CSDL Supabase PostgreSQL.
/// Kế thừa IdentityDbContext để quản lý bảng người dùng, vai trò và phân quyền của ASP.NET Core Identity,
/// đồng thời quản lý các thực thể nghiệp vụ: Recipes, Categories, Steps, Ingredients, Images, RefreshTokens.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Kích hoạt PostgreSQL Extensions trên Supabase phục vụ tìm kiếm toàn văn FTS tiếng Việt không dấu
        builder.HasPostgresExtension("unaccent");
        builder.HasPostgresExtension("pg_trgm");

        // Tự động quét và nạp toàn bộ cấu hình Fluent API trong Assembly Infrastructure
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Tự động quản lý dấu thời gian (Audit Logs) cho các thực thể kế thừa BaseEntity
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
