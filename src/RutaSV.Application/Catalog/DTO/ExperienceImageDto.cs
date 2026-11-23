namespace RutaSV.Application.Catalog.DTO;

public sealed record ExperienceImageDto(
    Guid Id,
    string ObjectKey,
    string Url,
    string? ThumbnailUrl,
    string? AltText,
    short SortOrder,
    bool IsCover,
    DateTime CreatedAt);
