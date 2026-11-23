using RutaSV.Domain.Identity.Model;

namespace RutaSV.Domain.Catalog.Model;

public class ExperienceImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExperienceId { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public short SortOrder { get; set; }
    public bool IsCover { get; set; }
    public DateTime? RemovedAt { get; set; }
    public Guid? RemovedBy { get; set; }
    public string? RemovalReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Experience? Experience { get; set; }
    public User? RemovedByUser { get; set; }
}
