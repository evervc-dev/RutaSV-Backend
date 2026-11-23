namespace RutaSV.Application.Catalog.DTO;

public sealed record GetMyExperiencesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Status = null,
    Guid? CompanyId = null);
