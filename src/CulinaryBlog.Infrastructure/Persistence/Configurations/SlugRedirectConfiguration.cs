using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class SlugRedirectConfiguration : IEntityTypeConfiguration<SlugRedirect>
{
    public void Configure(EntityTypeBuilder<SlugRedirect> builder)
    {
        builder.ToTable("SlugRedirects");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.EntityType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.OldSlug)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(s => s.OldSlug)
            .HasDatabaseName("IX_SlugRedirects_OldSlug");

        builder.HasIndex(s => new { s.EntityId, s.EntityType })
            .HasDatabaseName("IX_SlugRedirects_EntityId_EntityType");
    }
}
