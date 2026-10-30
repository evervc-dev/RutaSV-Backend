using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class EmailNotificationConfiguration : IEntityTypeConfiguration<EmailNotification>
{
    public void Configure(EntityTypeBuilder<EmailNotification> builder)
    {
        builder.ToTable("email_notification");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid");

        builder.Property(e => e.ReservationId)
            .HasColumnName("reservation_id")
            .HasColumnType("uuid");

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Recipient)
            .HasColumnName("recipient")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Body)
            .HasColumnName("body")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(EmailDeliveryStatus.PENDIENTE)
            .HasSentinel((EmailDeliveryStatus)(-1))
            .IsRequired();

        builder.Property(e => e.Attempts)
            .HasColumnName("attempts")
            .HasColumnType("smallint")
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.Property(e => e.Error)
            .HasColumnName("error")
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(e => e.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("ix_email_notification_status_created_at");

        builder.HasOne(e => e.User)
            .WithMany(u => u.EmailNotifications)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
