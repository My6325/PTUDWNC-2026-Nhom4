using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Slug)
            .HasMaxLength(220)
            .IsRequired();

        builder.HasIndex(r => r.Slug)
            .IsUnique();

        builder.Property(r => r.Description)
            .HasColumnType("text");

        builder.Property(r => r.Instructions)
            .HasColumnType("text");

        builder.Property(r => r.AuthorId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(r => r.Difficulty)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        builder.OwnsOne(r => r.Nutrition, nutrition =>
        {
            nutrition.Property(n => n.Calories).HasColumnName("Nutrition_Calories");
            nutrition.Property(n => n.Protein).HasColumnName("Nutrition_Protein").HasPrecision(10, 2);
            nutrition.Property(n => n.Carbohydrates).HasColumnName("Nutrition_Carbohydrates").HasPrecision(10, 2);
            nutrition.Property(n => n.Fat).HasColumnName("Nutrition_Fat").HasPrecision(10, 2);
            nutrition.Property(n => n.Fiber).HasColumnName("Nutrition_Fiber").HasPrecision(10, 2);
            nutrition.Property(n => n.Sodium).HasColumnName("Nutrition_Sodium").HasPrecision(10, 2);
        });

        builder.HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Ingredients)
            .WithOne()
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Images)
            .WithOne()
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
