namespace RutaSV.Application.Catalog.DTO;

public sealed record CreateExperienceImageRequest(
    string? ObjectKey = null,
    string? Url = null,
    string? ThumbnailUrl = null,
    string? AltText = null,
    short SortOrder = 0,
    bool IsCover = false);
