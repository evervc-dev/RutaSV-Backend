namespace RutaSV.Domain.Catalog.Model;

public class ExperienceCategory
{
    public Guid ExperienceId { get; set; }
    public int CategoryId { get; set; }
    public bool IsPrimary { get; set; }

    public Experience? Experience { get; set; }
    public Category? Category { get; set; }
}
