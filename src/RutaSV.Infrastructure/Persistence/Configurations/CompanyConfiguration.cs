using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("company");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.OperatorId)
            .HasColumnName("operator_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.TradeName)
            .HasColumnName("trade_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(x => x.ContactEmail)
            .HasColumnName("contact_email")
            .HasMaxLength(150);

        builder.Property(x => x.ContactPhone)
            .HasColumnName("contact_phone")
            .HasMaxLength(20);

        builder.Property(x => x.Whatsapp)
            .HasColumnName("whatsapp")
            .HasMaxLength(20);

        builder.Property(x => x.Website)
            .HasColumnName("website")
            .HasMaxLength(300);

        builder.Property(x => x.LogoUrl)
            .HasColumnName("logo_url")
            .HasMaxLength(500);

        builder.Property(x => x.MunicipalityId)
            .HasColumnName("municipality_id")
            .HasColumnType("integer");

        builder.Property(x => x.Address)
            .HasColumnName("address")
            .HasMaxLength(300);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(15)
            .HasDefaultValue(CompanyStatus.PENDIENTE)
            .HasSentinel((CompanyStatus)(-1))
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(500);

        builder.Property(x => x.ApprovedBy)
            .HasColumnName("approved_by")
            .HasColumnType("uuid");

        builder.Property(x => x.ApprovedAt)
            .HasColumnName("approved_at")
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

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz");

        builder.HasOne(x => x.Operator)
            .WithMany(u => u.Companies)
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
