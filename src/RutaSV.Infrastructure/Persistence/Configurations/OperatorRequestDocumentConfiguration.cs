using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Identity.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class OperatorRequestDocumentConfiguration : IEntityTypeConfiguration<OperatorRequestDocument>
{
    public void Configure(EntityTypeBuilder<OperatorRequestDocument> builder)
    {
        builder.ToTable("operator_request_document");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.RequestId)
            .HasColumnName("request_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(300);

        builder.Property(x => x.Url)
            .HasColumnName("url")
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(x => x.RequestId)
            .HasDatabaseName("ix_operator_request_document_request_id");
    }
}
