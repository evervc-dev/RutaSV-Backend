namespace RutaSV.Domain.Catalog.Model;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public short SortOrder { get; set; }

    public ICollection<ExperienceCategory> ExperienceCategories { get; set; } = [];
}
