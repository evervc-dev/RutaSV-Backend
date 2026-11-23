namespace RutaSV.Application.Catalog.DTO;

public sealed record CategoryDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? Icon,
    short SortOrder,
    bool IsPrimary = false);
