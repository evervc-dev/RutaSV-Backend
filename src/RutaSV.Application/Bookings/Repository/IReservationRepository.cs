using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Bookings.Model;

namespace RutaSV.Application.Bookings.Repository;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Reservation?> GetByCodeWithDetailsAsync(string code, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Reservation> Items, int TotalRecords)> GetByVisitorPagedAsync(
        Guid visitorId,
        int pageNumber,
        int pageSize,
        ReservationStatus? status = null,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Reservation> Items, int TotalRecords)> GetByOperatorPagedAsync(
        Guid? operatorId,
        int pageNumber,
        int pageSize,
        ReservationStatus? status = null,
        Guid? experienceId = null,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetActiveFutureByAvailabilityAsync(
        Guid availabilityId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetActiveFutureByExperienceAsync(
        Guid experienceId,
        DateOnly minDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task AddHistoryAsync(ReservationHistory history, CancellationToken cancellationToken = default);
    Task AddRefundAsync(Refund refund, CancellationToken cancellationToken = default);
    Task UpdateRefundAsync(Refund refund, CancellationToken cancellationToken = default);
    Task AddIncidentAsync(ReservationIncident incident, CancellationToken cancellationToken = default);
    Task UpdateIncidentAsync(ReservationIncident incident, CancellationToken cancellationToken = default);

    Task<Refund?> GetRefundByIdOrCodeAsync(string idOrCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Refund>> GetRefundsByOperatorAsync(Guid? operatorId, CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
