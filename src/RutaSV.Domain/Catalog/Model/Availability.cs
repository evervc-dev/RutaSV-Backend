using RutaSV.Domain.Catalog.Enum;

namespace RutaSV.Domain.Catalog.Model;

public class Availability
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExperienceId { get; set; }
    public Guid? ScheduleId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int TotalCapacity { get; set; }
    public int ReservedCapacity { get; set; }
    public decimal? SpecialPrice { get; set; }
    public AvailabilityStatus Status { get; set; } = AvailabilityStatus.ABIERTA;
    public string? CancellationReason { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Experience? Experience { get; set; }
    public ExperienceSchedule? Schedule { get; set; }
}
