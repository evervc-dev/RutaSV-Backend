using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        _ = builder.ToTable("experience", t =>
        {
            _ = t.HasCheckConstraint("ck_experience_price", "price >= 0");
            _ = t.HasCheckConstraint("ck_experience_duration_minutes", "duration_minutes IS NULL OR duration_minutes > 0");
            _ = t.HasCheckConstraint("ck_experience_min_people", "min_people > 0");
            _ = t.HasCheckConstraint(
                "ck_experience_capacity",
                "(NOT has_limited_capacity) OR (max_people IS NOT NULL AND max_people >= min_people)");
            _ = t.HasCheckConstraint("ck_experience_latitude", "latitude >= -90 AND latitude <= 90");
            _ = t.HasCheckConstraint("ck_experience_longitude", "longitude >= -180 AND longitude <= 180");
        });

        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        _ = builder.Property(x => x.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        _ = builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(150)
            .IsRequired();

        _ = builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(180)
            .IsRequired();

        _ = builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("uq_experience_slug");

        _ = builder.Property(x => x.ShortDescription)
            .HasColumnName("short_description")
            .HasMaxLength(300)
            .IsRequired();

        _ = builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        _ = builder.Property(x => x.Includes)
            .HasColumnName("includes")
            .HasColumnType("text");

        _ = builder.Property(x => x.Excludes)
            .HasColumnName("excludes")
            .HasColumnType("text");

        _ = builder.Property(x => x.WhatToBring)
            .HasColumnName("what_to_bring")
            .HasColumnType("text");

        _ = builder.Property(x => x.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(10,2)")
            .IsRequired();

        _ = builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasDefaultValue("USD")
            .IsRequired();

        _ = builder.Property(x => x.DurationMinutes)
            .HasColumnName("duration_minutes")
            .HasColumnType("integer");

        _ = builder.Property(x => x.Difficulty)
            .HasColumnName("difficulty")
            .HasConversion<string>()
            .HasMaxLength(10);

        _ = builder.Property(x => x.MinAge)
            .HasColumnName("min_age")
            .HasColumnType("smallint");

        _ = builder.Property(x => x.ServiceLanguage)
            .HasColumnName("service_language")
            .HasMaxLength(50);

        _ = builder.Property(x => x.HasLimitedCapacity)
            .HasColumnName("has_limited_capacity")
            .HasDefaultValue(true)
            .IsRequired();

        _ = builder.Property(x => x.MinPeople)
            .HasColumnName("min_people")
            .HasColumnType("smallint")
            .HasDefaultValue((short)1)
            .IsRequired();

        _ = builder.Property(x => x.MaxPeople)
            .HasColumnName("max_people")
            .HasColumnType("smallint");

        _ = builder.Property(x => x.MunicipalityId)
            .HasColumnName("municipality_id")
            .HasColumnType("integer")
            .IsRequired();

        _ = builder.Property(x => x.ReferenceAddress)
            .HasColumnName("reference_address")
            .HasMaxLength(300);

        _ = builder.Property(x => x.Latitude)
            .HasColumnName("latitude")
            .HasColumnType("numeric(9,6)")
            .IsRequired();

        _ = builder.Property(x => x.Longitude)
            .HasColumnName("longitude")
            .HasColumnType("numeric(9,6)")
            .IsRequired();

        _ = builder.Property(x => x.AutoConfirmation)
            .HasColumnName("auto_confirmation")
            .HasDefaultValue(false)
            .IsRequired();

        _ = builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ExperienceStatus.BORRADOR)
            .HasSentinel((ExperienceStatus)(-1))
            .IsRequired();

        _ = builder.Property(x => x.WithdrawnBy)
            .HasColumnName("withdrawn_by")
            .HasColumnType("uuid");

        _ = builder.Property(x => x.WithdrawnAt)
            .HasColumnName("withdrawn_at")
            .HasColumnType("timestamptz");

        _ = builder.Property(x => x.WithdrawalReason)
            .HasColumnName("withdrawal_reason")
            .HasMaxLength(500);

        _ = builder.Property(x => x.AverageRating)
            .HasColumnName("average_rating")
            .HasColumnType("numeric(2,1)")
            .HasDefaultValue(0m)
            .IsRequired();

        _ = builder.Property(x => x.TotalReviews)
            .HasColumnName("total_reviews")
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsRequired();

        _ = builder.Property(x => x.PublishedAt)
            .HasColumnName("published_at")
            .HasColumnType("timestamptz");

        _ = builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        _ = builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        _ = builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz");

        _ = builder.HasIndex(x => new { x.Status, x.MunicipalityId })
            .HasDatabaseName("ix_experience_status_municipality_id");

        _ = builder.HasIndex(x => new { x.Status, x.Price })
            .HasDatabaseName("ix_experience_status_price");

        _ = builder.HasIndex(x => new { x.Status, x.AverageRating })
            .HasDatabaseName("ix_experience_status_average_rating");

        _ = builder.HasOne(x => x.Company)
            .WithMany(c => c.Experiences)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.Municipality)
            .WithMany(m => m.Experiences)
            .HasForeignKey(x => x.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.WithdrawnByUser)
            .WithMany()
            .HasForeignKey(x => x.WithdrawnBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
