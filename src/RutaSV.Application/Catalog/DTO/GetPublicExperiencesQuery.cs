namespace RutaSV.Application.Catalog.DTO;

public sealed record GetPublicExperiencesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    int? CategoryId = null,
    short? DepartmentId = null,
    int? MunicipalityId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    decimal? MinRating = null,
    string? Difficulty = null,
    int? MinDurationMinutes = null,
    int? MaxDurationMinutes = null,
    bool? HasLimitedCapacity = null,
    string? SortBy = null);
