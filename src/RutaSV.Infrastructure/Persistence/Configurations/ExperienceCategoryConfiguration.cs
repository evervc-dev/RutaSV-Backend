using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class ExperienceCategoryConfiguration : IEntityTypeConfiguration<ExperienceCategory>
{
    public void Configure(EntityTypeBuilder<ExperienceCategory> builder)
    {
        _ = builder.ToTable("experience_category");

        _ = builder.HasKey(ec => new { ec.ExperienceId, ec.CategoryId });

        _ = builder.Property(ec => ec.ExperienceId)
            .HasColumnName("experience_id")
            .HasColumnType("uuid")
            .IsRequired();

        _ = builder.Property(ec => ec.CategoryId)
            .HasColumnName("category_id")
            .HasColumnType("integer")
            .IsRequired();

        _ = builder.Property(ec => ec.IsPrimary)
            .HasColumnName("is_primary")
            .HasDefaultValue(false)
            .IsRequired();

        _ = builder.HasIndex(ec => new { ec.CategoryId, ec.ExperienceId })
            .HasDatabaseName("ix_experience_category_category_id_experience_id");

        _ = builder.HasOne(ec => ec.Experience)
            .WithMany(e => e.Categories)
            .HasForeignKey(ec => ec.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(ec => ec.Category)
            .WithMany(c => c.ExperienceCategories)
            .HasForeignKey(ec => ec.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
