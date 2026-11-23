using RutaSV.Domain.Identity.Model;

namespace RutaSV.Domain.Catalog.Model;

public class Favorite
{
    public Guid UserId { get; set; }
    public Guid ExperienceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Experience? Experience { get; set; }
}
