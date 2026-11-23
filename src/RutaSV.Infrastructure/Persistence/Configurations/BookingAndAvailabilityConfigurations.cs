using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Bookings.Model;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class ExperienceScheduleConfiguration : IEntityTypeConfiguration<ExperienceSchedule>
{
    public void Configure(EntityTypeBuilder<ExperienceSchedule> builder)
    {
        _ = builder.ToTable("experience_schedule", t =>
        {
            _ = t.HasCheckConstraint("ck_experience_schedule_dow", "day_of_week >= 0 AND day_of_week <= 6");
            _ = t.HasCheckConstraint("ck_experience_schedule_capacity", "default_capacity > 0");
        });

        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        _ = builder.Property(x => x.ExperienceId).HasColumnName("experience_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.DayOfWeek).HasColumnName("day_of_week").HasColumnType("smallint").IsRequired();
        _ = builder.Property(x => x.StartTime).HasColumnName("start_time").HasColumnType("time").IsRequired();
        _ = builder.Property(x => x.EndTime).HasColumnName("end_time").HasColumnType("time");
        _ = builder.Property(x => x.DefaultCapacity).HasColumnName("default_capacity").HasDefaultValue(10).IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        _ = builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        _ = builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        _ = builder.HasOne(x => x.Experience)
            .WithMany(e => e.Schedules)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        _ = builder.ToTable("availability", t =>
        {
            _ = t.HasCheckConstraint("ck_availability_total_capacity", "total_capacity > 0");
            _ = t.HasCheckConstraint("ck_availability_reserved_capacity", "reserved_capacity >= 0 AND reserved_capacity <= total_capacity");
            _ = t.HasCheckConstraint("ck_availability_special_price", "special_price IS NULL OR special_price >= 0");
        });

        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        _ = builder.Property(x => x.ExperienceId).HasColumnName("experience_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.ScheduleId).HasColumnName("schedule_id").HasColumnType("uuid");
        _ = builder.Property(x => x.Date).HasColumnName("date").HasColumnType("date").IsRequired();
        _ = builder.Property(x => x.StartTime).HasColumnName("start_time").HasColumnType("time").IsRequired();
        _ = builder.Property(x => x.EndTime).HasColumnName("end_time").HasColumnType("time");
        _ = builder.Property(x => x.TotalCapacity).HasColumnName("total_capacity").IsRequired();
        _ = builder.Property(x => x.ReservedCapacity).HasColumnName("reserved_capacity").HasDefaultValue(0).IsRequired();
        _ = builder.Property(x => x.SpecialPrice).HasColumnName("special_price").HasColumnType("numeric(10,2)");
        _ = builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(15)
            .HasDefaultValue(AvailabilityStatus.ABIERTA)
            .HasSentinel((AvailabilityStatus)(-1))
            .IsRequired();
        _ = builder.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        _ = builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1).IsConcurrencyToken().IsRequired();
        _ = builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        _ = builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        _ = builder.HasIndex(x => new { x.ExperienceId, x.Date, x.StartTime })
            .IsUnique()
            .HasDatabaseName("uq_availability_experience_date_start");

        _ = builder.HasIndex(x => new { x.ExperienceId, x.Date, x.Status })
            .HasDatabaseName("ix_availability_experience_date_status");

        _ = builder.HasOne(x => x.Experience)
            .WithMany(e => e.Availabilities)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.Schedule)
            .WithMany()
            .HasForeignKey(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        _ = builder.ToTable("favorite");
        _ = builder.HasKey(x => new { x.UserId, x.ExperienceId });

        _ = builder.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.ExperienceId).HasColumnName("experience_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        _ = builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.Experience)
            .WithMany(e => e.Favorites)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        _ = builder.ToTable("reservation", t =>
        {
            _ = t.HasCheckConstraint("ck_reservation_people_count", "people_count > 0");
            _ = t.HasCheckConstraint("ck_reservation_unit_price", "unit_price >= 0");
            _ = t.HasCheckConstraint("ck_reservation_total", "total >= 0");
        });

        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        _ = builder.Property(x => x.ReservationCode).HasColumnName("reservation_code").HasMaxLength(30).IsRequired();
        _ = builder.HasIndex(x => x.ReservationCode).IsUnique().HasDatabaseName("uq_reservation_code");

        _ = builder.Property(x => x.ExperienceId).HasColumnName("experience_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.AvailabilityId).HasColumnName("availability_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.VisitorId).HasColumnName("visitor_id").HasColumnType("uuid");
        _ = builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(15)
            .HasDefaultValue(ReservationSource.WEB)
            .HasSentinel((ReservationSource)(-1))
            .IsRequired();
        _ = builder.Property(x => x.PeopleCount).HasColumnName("people_count").HasColumnType("smallint").IsRequired();
        _ = builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(10,2)").IsRequired();
        _ = builder.Property(x => x.Total).HasColumnName("total").HasColumnType("numeric(10,2)").IsRequired();
        _ = builder.Property(x => x.Currency).HasColumnName("currency").HasColumnType("character(3)").HasDefaultValue("USD").IsRequired();
        _ = builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(25)
            .HasDefaultValue(ReservationStatus.PENDIENTE)
            .HasSentinel((ReservationStatus)(-1))
            .IsRequired();
        _ = builder.Property(x => x.PaymentStatus)
            .HasColumnName("payment_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(PaymentStatus.PAGADO_DEMO)
            .HasSentinel((PaymentStatus)(-1))
            .IsRequired();
        _ = builder.Property(x => x.PaymentReference).HasColumnName("payment_reference").HasMaxLength(100);
        _ = builder.Property(x => x.ContactName).HasColumnName("contact_name").HasMaxLength(150).IsRequired();
        _ = builder.Property(x => x.ContactEmail).HasColumnName("contact_email").HasMaxLength(150);
        _ = builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        _ = builder.Property(x => x.SpecialNotes).HasColumnName("special_notes").HasMaxLength(500);
        _ = builder.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        _ = builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamptz");
        _ = builder.Property(x => x.CancelledBy).HasColumnName("cancelled_by").HasColumnType("uuid");
        _ = builder.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at").HasColumnType("timestamptz");
        _ = builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        _ = builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        _ = builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        _ = builder.HasIndex(x => new { x.VisitorId, x.CreatedAt }).HasDatabaseName("ix_reservation_visitor_created_at");
        _ = builder.HasIndex(x => new { x.ExperienceId, x.Status }).HasDatabaseName("ix_reservation_experience_status");
        _ = builder.HasIndex(x => x.AvailabilityId).HasDatabaseName("ix_reservation_availability_id");

        _ = builder.HasOne(x => x.Experience).WithMany().HasForeignKey(x => x.ExperienceId).OnDelete(DeleteBehavior.Restrict);
        _ = builder.HasOne(x => x.Availability).WithMany().HasForeignKey(x => x.AvailabilityId).OnDelete(DeleteBehavior.Restrict);
        _ = builder.HasOne(x => x.Visitor).WithMany().HasForeignKey(x => x.VisitorId).OnDelete(DeleteBehavior.SetNull);
        _ = builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        _ = builder.HasOne(x => x.CancelledByUser).WithMany().HasForeignKey(x => x.CancelledBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ReservationHistoryConfiguration : IEntityTypeConfiguration<ReservationHistory>
{
    public void Configure(EntityTypeBuilder<ReservationHistory> builder)
    {
        _ = builder.ToTable("reservation_history");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        _ = builder.Property(x => x.ReservationId).HasColumnName("reservation_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.PreviousStatus).HasColumnName("previous_status").HasConversion<string>().HasMaxLength(25);
        _ = builder.Property(x => x.NewStatus).HasColumnName("new_status").HasConversion<string>().HasMaxLength(25).IsRequired();
        _ = builder.Property(x => x.ChangedBy).HasColumnName("changed_by").HasColumnType("uuid");
        _ = builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        _ = builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        _ = builder.HasOne(x => x.Reservation)
            .WithMany(r => r.History)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.ChangedByUser)
            .WithMany()
            .HasForeignKey(x => x.ChangedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        _ = builder.ToTable("refund", t =>
        {
            _ = t.HasCheckConstraint("ck_refund_amount", "amount >= 0");
            _ = t.HasCheckConstraint("ck_refund_percentage", "percentage >= 0 AND percentage <= 100");
        });

        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        _ = builder.Property(x => x.RefundCode).HasColumnName("refund_code").HasMaxLength(30).IsRequired();
        _ = builder.HasIndex(x => x.RefundCode).IsUnique().HasDatabaseName("uq_refund_code");
        _ = builder.Property(x => x.ReservationId).HasColumnName("reservation_id").HasColumnType("uuid").IsRequired();
        _ = builder.HasIndex(x => x.ReservationId).IsUnique().HasDatabaseName("uq_refund_reservation_id");
        _ = builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(10,2)").IsRequired();
        _ = builder.Property(x => x.Percentage).HasColumnName("percentage").HasColumnType("smallint").IsRequired();
        _ = builder.Property(x => x.Reason).HasColumnName("reason").HasConversion<string>().HasMaxLength(30).IsRequired();
        _ = builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(15)
            .HasDefaultValue(RefundStatus.EN_PROCESO)
            .HasSentinel((RefundStatus)(-1))
            .IsRequired();
        _ = builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        _ = builder.Property(x => x.RequestedAt).HasColumnName("requested_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        _ = builder.Property(x => x.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamptz");
        _ = builder.Property(x => x.ProcessedBy).HasColumnName("processed_by").HasColumnType("uuid");
        _ = builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        _ = builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        _ = builder.HasOne(x => x.Reservation)
            .WithOne(r => r.Refund)
            .HasForeignKey<Refund>(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.ProcessedByUser)
            .WithMany()
            .HasForeignKey(x => x.ProcessedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ReservationIncidentConfiguration : IEntityTypeConfiguration<ReservationIncident>
{
    public void Configure(EntityTypeBuilder<ReservationIncident> builder)
    {
        _ = builder.ToTable("reservation_incident");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
        _ = builder.Property(x => x.ReservationId).HasColumnName("reservation_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.Cause).HasColumnName("cause").HasConversion<string>().HasMaxLength(30).IsRequired();
        _ = builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(IncidentStatus.PENDIENTE)
            .HasSentinel((IncidentStatus)(-1))
            .IsRequired();
        _ = builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
        _ = builder.Property(x => x.OriginalAvailabilityId).HasColumnName("original_availability_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(x => x.NewAvailabilityId).HasColumnName("new_availability_id").HasColumnType("uuid");
        _ = builder.Property(x => x.DecisionDeadline).HasColumnName("decision_deadline").HasColumnType("timestamptz").IsRequired();
        _ = builder.Property(x => x.ResolvedAt).HasColumnName("resolved_at").HasColumnType("timestamptz");
        _ = builder.Property(x => x.AutoResolved).HasColumnName("auto_resolved").HasDefaultValue(false).IsRequired();
        _ = builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();
        _ = builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").HasDefaultValueSql("now()").IsRequired();

        _ = builder.HasIndex(x => x.ReservationId)
            .IsUnique()
            .HasFilter("status = 'PENDIENTE'")
            .HasDatabaseName("uq_reservation_incident_pending");

        _ = builder.HasOne(x => x.Reservation)
            .WithMany(r => r.Incidents)
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.OriginalAvailability)
            .WithMany()
            .HasForeignKey(x => x.OriginalAvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasOne(x => x.NewAvailability)
            .WithMany()
            .HasForeignKey(x => x.NewAvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
