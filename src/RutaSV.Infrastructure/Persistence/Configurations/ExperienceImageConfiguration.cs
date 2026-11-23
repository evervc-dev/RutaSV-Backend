using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class ExperienceImageConfiguration : IEntityTypeConfiguration<ExperienceImage>
{
    public void Configure(EntityTypeBuilder<ExperienceImage> builder)
    {
        _ = builder.ToTable("experience_image");

        _ = builder.HasKey(i => i.Id);

        _ = builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        _ = builder.Property(i => i.ExperienceId)
            .HasColumnName("experience_id")
            .HasColumnType("uuid")
            .IsRequired();

        _ = builder.Property(i => i.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(300)
            .IsRequired();

        _ = builder.Property(i => i.Url)
            .HasColumnName("url")
            .HasMaxLength(500)
            .IsRequired();

        _ = builder.Property(i => i.ThumbnailUrl)
            .HasColumnName("thumbnail_url")
            .HasMaxLength(500);

        _ = builder.Property(i => i.AltText)
            .HasColumnName("alt_text")
            .HasMaxLength(200);

        _ = builder.Property(i => i.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("smallint")
            .HasDefaultValue((short)0)
            .IsRequired();

        _ = builder.Property(i => i.IsCover)
            .HasColumnName("is_cover")
            .HasDefaultValue(false)
            .IsRequired();

        _ = builder.Property(i => i.RemovedAt)
            .HasColumnName("removed_at")
            .HasColumnType("timestamptz");

        _ = builder.Property(i => i.RemovedBy)
            .HasColumnName("removed_by")
            .HasColumnType("uuid");

        _ = builder.Property(i => i.RemovalReason)
            .HasColumnName("removal_reason")
            .HasMaxLength(500);

        _ = builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        _ = builder.HasIndex(i => i.ExperienceId)
            .HasDatabaseName("ix_experience_image_experience_id");

        _ = builder.HasOne(i => i.Experience)
            .WithMany(e => e.Images)
            .HasForeignKey(i => i.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(i => i.RemovedByUser)
            .WithMany()
            .HasForeignKey(i => i.RemovedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
