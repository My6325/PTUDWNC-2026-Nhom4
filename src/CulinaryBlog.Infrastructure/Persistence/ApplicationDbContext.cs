using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>
/// DbContext trung tâm của ứng dụng Culinary Blog kết nối CSDL Supabase PostgreSQL.
/// Kế thừa IdentityDbContext để quản lý bảng người dùng, vai trò và phân quyền của ASP.NET Core Identity,
/// đồng thời quản lý các thực thể nghiệp vụ: Recipes, Categories, Steps, Ingredients, Images, RefreshTokens.
/// Hiện thực interface IApplicationDbContext phục vụ tầng Application theo chuẩn Clean Architecture.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        ChangeTracker.Tracked += OnEntityTracked;
    }

    private void OnEntityTracked(object? sender, EntityTrackedEventArgs e)
    {
        // Khi một thực thể con kế thừa BaseEntity được thêm vào qua navigation collection của thực thể cha đang được track,
        // do BaseEntity.Id đã được khởi tạo sẵn Guid.NewGuid(), EF Core mặc định gán State = Modified thay vì Added.
        // Điều này dẫn đến câu lệnh UPDATE vào bản ghi chưa tồn tại và gây lỗi DbUpdateConcurrencyException (409 Conflict).
        // Hook này tự động ép State = Added cho các thực thể chưa từng được query từ database.
        if (!e.FromQuery && e.Entry.Entity is BaseEntity && e.Entry.State == EntityState.Modified)
        {
            e.Entry.State = EntityState.Added;
        }
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

        if (Database.IsNpgsql())
        {
            // Kích hoạt PostgreSQL Extensions trên Supabase phục vụ tìm kiếm toàn văn FTS tiếng Việt không dấu
            builder.HasPostgresExtension("unaccent");
            builder.HasPostgresExtension("pg_trgm");
        }

        // Tự động quét và nạp toàn bộ cấu hình Fluent API trong Assembly Infrastructure
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        if (!Database.IsNpgsql())
        {
            builder.Entity<Recipe>().Ignore("SearchVector");
        }
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
