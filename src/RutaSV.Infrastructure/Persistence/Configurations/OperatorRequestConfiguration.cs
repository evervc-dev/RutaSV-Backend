using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class OperatorRequestConfiguration : IEntityTypeConfiguration<OperatorRequest>
{
    public void Configure(EntityTypeBuilder<OperatorRequest> builder)
    {
        builder.ToTable("operator_request");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(OperatorRequestStatus.PENDIENTE)
            .HasSentinel((OperatorRequestStatus)(-1))
            .IsRequired();

        builder.Property(x => x.Message)
            .HasColumnName("message")
            .HasMaxLength(500);

        builder.Property(x => x.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(500);

        builder.Property(x => x.ReviewedBy)
            .HasColumnName("reviewed_by")
            .HasColumnType("uuid");

        builder.Property(x => x.ReviewedAt)
            .HasColumnName("reviewed_at")
            .HasColumnType("timestamptz");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("status = 'PENDIENTE'");

        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("ix_operator_request_status_created_at");

        builder.HasOne(x => x.User)
            .WithMany(u => u.OperatorRequests)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Company)
            .WithMany(c => c.OperatorRequests)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Documents)
            .WithOne(d => d.OperatorRequest)
            .HasForeignKey(d => d.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
