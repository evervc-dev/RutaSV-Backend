namespace RutaSV.Application.Catalog.DTO;

public sealed record MunicipalityDto(
    int Id,
    short DepartmentId,
    string DepartmentName,
    string Name);
