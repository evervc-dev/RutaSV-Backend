namespace RutaSV.Application.Catalog.DTO;

public sealed record AvailabilityDto(
    Guid Id,
    Guid ExperienceId,
    string ExperienceTitle,
    string ExperienceSlug,
    Guid? ScheduleId,
    string Date,
    string StartTime,
    string? EndTime,
    int TotalCapacity,
    int ReservedCapacity,
    int AvailableSpots,
    decimal BasePrice,
    decimal? SpecialPrice,
    decimal EffectivePrice,
    string Currency,
    string Status,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateAvailabilityRequest(
    string Date,
    string StartTime,
    string? EndTime = null,
    int TotalCapacity = 10,
    decimal? SpecialPrice = null);

public sealed record UpdateAvailabilityRequest(
    string Date,
    string StartTime,
    string? EndTime = null,
    int TotalCapacity = 10,
    decimal? SpecialPrice = null,
    string? Status = null);

public sealed record CancelAvailabilityRequest(
    string? Reason = null);

public sealed record ExperienceScheduleDto(
    Guid Id,
    Guid ExperienceId,
    short DayOfWeek,
    string StartTime,
    string? EndTime,
    int DefaultCapacity,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CreateExperienceScheduleRequest(
    short DayOfWeek,
    string StartTime,
    string? EndTime = null,
    int DefaultCapacity = 10);

public sealed record GenerateAvailabilitiesRequest(
    string FromDate,
    string ToDate,
    int? OverrideCapacity = null,
    decimal? SpecialPrice = null);

public sealed record WithdrawExperienceRequest(
    string Reason);

public sealed record FavoriteItemDto(
    Guid ExperienceId,
    DateTime FavoritedAt,
    ExperienceSummaryDto Experience);
