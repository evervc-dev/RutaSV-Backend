namespace RutaSV.Domain.Catalog.Model;

public class Municipality
{
    public int Id { get; set; }
    public short DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Department? Department { get; set; }
    public ICollection<Experience> Experiences { get; set; } = [];
}
