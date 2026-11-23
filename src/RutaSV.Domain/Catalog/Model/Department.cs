namespace RutaSV.Domain.Catalog.Model;

public class Department
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Municipality> Municipalities { get; set; } = [];
}
