namespace RutaSV.Domain.Catalog.Model;

public class ExperienceSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExperienceId { get; set; }
    public short DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int DefaultCapacity { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Experience? Experience { get; set; }
}
