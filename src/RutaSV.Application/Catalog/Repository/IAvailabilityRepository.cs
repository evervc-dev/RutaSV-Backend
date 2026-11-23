using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Application.Catalog.Repository;

public interface IAvailabilityRepository
{
    Task<Availability?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Availability?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Availability>> GetByExperienceAsync(
        Guid experienceId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        bool includeUnavailable = false,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Availability>> GetByOperatorAsync(
        Guid? operatorId,
        Guid? experienceId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsSlotAsync(
        Guid experienceId,
        DateOnly date,
        TimeOnly startTime,
        Guid? excludeAvailabilityId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Availability availability, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Availability> availabilities, CancellationToken cancellationToken = default);
    Task UpdateAsync(Availability availability, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExperienceSchedule>> GetSchedulesByExperienceAsync(
        Guid experienceId,
        CancellationToken cancellationToken = default);
    Task<ExperienceSchedule?> GetScheduleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddScheduleAsync(ExperienceSchedule schedule, CancellationToken cancellationToken = default);
    Task DeleteScheduleAsync(ExperienceSchedule schedule, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
