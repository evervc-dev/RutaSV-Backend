using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using RutaSV.Application.Bookings.Repository;
using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Bookings.Model;
using RutaSV.Infrastructure.Persistence;

namespace RutaSV.Infrastructure.Repositories;

public sealed class ReservationRepository(ApplicationDbContext dbContext) : IReservationRepository
{
    private IQueryable<Reservation> BaseDetailQuery()
    {
        return dbContext.Reservations
            .Include(r => r.Experience)
                .ThenInclude(e => e!.Company)
                    .ThenInclude(c => c!.Operator)
            .Include(r => r.Experience)
                .ThenInclude(e => e!.Municipality)
                    .ThenInclude(m => m!.Department)
            .Include(r => r.Experience)
                .ThenInclude(e => e!.Images)
            .Include(r => r.Availability)
            .Include(r => r.Visitor)
            .Include(r => r.Refund)
            .Include(r => r.History)
            .Include(r => r.Incidents);
    }

    public Task<Reservation?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return BaseDetailQuery().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<Reservation?> GetByCodeWithDetailsAsync(string code, CancellationToken cancellationToken = default)
    {
        string trimmed = code.Trim();
        return BaseDetailQuery().FirstOrDefaultAsync(r => r.ReservationCode == trimmed, cancellationToken);
    }

    public async Task<(IReadOnlyList<Reservation> Items, int TotalRecords)> GetByVisitorPagedAsync(
        Guid visitorId,
        int pageNumber,
        int pageSize,
        ReservationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Reservation> query = BaseDetailQuery()
            .AsNoTracking()
            .Where(r => r.VisitorId == visitorId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        int total = await query.CountAsync(cancellationToken);
        List<Reservation> items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Reservation> Items, int TotalRecords)> GetByOperatorPagedAsync(
        Guid? operatorId,
        int pageNumber,
        int pageSize,
        ReservationStatus? status = null,
        Guid? experienceId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Reservation> query = BaseDetailQuery().AsNoTracking();

        if (operatorId.HasValue)
        {
            query = query.Where(r => r.Experience != null
                && r.Experience.Company != null
                && r.Experience.Company.OperatorId == operatorId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (experienceId.HasValue)
        {
            query = query.Where(r => r.ExperienceId == experienceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim()}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.ReservationCode, pattern)
                || EF.Functions.ILike(r.ContactName, pattern)
                || (r.ContactEmail != null && EF.Functions.ILike(r.ContactEmail, pattern))
                || (r.Experience != null && EF.Functions.ILike(r.Experience.Title, pattern)));
        }

        int total = await query.CountAsync(cancellationToken);
        List<Reservation> items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Reservation>> GetActiveFutureByAvailabilityAsync(
        Guid availabilityId,
        CancellationToken cancellationToken = default)
    {
        return await BaseDetailQuery()
            .Where(r => r.AvailabilityId == availabilityId
                && (r.Status == ReservationStatus.PENDIENTE || r.Status == ReservationStatus.CONFIRMADA))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetActiveFutureByExperienceAsync(
        Guid experienceId,
        DateOnly minDate,
        CancellationToken cancellationToken = default)
    {
        return await BaseDetailQuery()
            .Where(r => r.ExperienceId == experienceId
                && r.Availability != null
                && r.Availability.Date >= minDate
                && (r.Status == ReservationStatus.PENDIENTE || r.Status == ReservationStatus.CONFIRMADA))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        _ = await dbContext.Reservations.AddAsync(reservation, cancellationToken);
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = dbContext.Reservations.Update(reservation);
        return Task.CompletedTask;
    }

    public async Task AddHistoryAsync(ReservationHistory history, CancellationToken cancellationToken = default)
    {
        _ = await dbContext.ReservationHistories.AddAsync(history, cancellationToken);
    }

    public async Task AddRefundAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        _ = await dbContext.Refunds.AddAsync(refund, cancellationToken);
    }

    public Task UpdateRefundAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = dbContext.Refunds.Update(refund);
        return Task.CompletedTask;
    }

    public async Task AddIncidentAsync(ReservationIncident incident, CancellationToken cancellationToken = default)
    {
        _ = await dbContext.ReservationIncidents.AddAsync(incident, cancellationToken);
    }

    public Task UpdateIncidentAsync(ReservationIncident incident, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = dbContext.ReservationIncidents.Update(incident);
        return Task.CompletedTask;
    }

    public async Task<Refund?> GetRefundByIdOrCodeAsync(string idOrCode, CancellationToken cancellationToken = default)
    {
        string trimmed = idOrCode.Trim();
        IQueryable<Refund> query = dbContext.Refunds
            .Include(rf => rf.Reservation)
                .ThenInclude(r => r!.Experience)
                    .ThenInclude(e => e!.Company);

        return Guid.TryParse(trimmed, out Guid id)
            ? await query.FirstOrDefaultAsync(rf => rf.Id == id, cancellationToken)
            : await query.FirstOrDefaultAsync(rf => rf.RefundCode == trimmed, cancellationToken);
    }

    public async Task<IReadOnlyList<Refund>> GetRefundsByOperatorAsync(
        Guid? operatorId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Refund> query = dbContext.Refunds
            .AsNoTracking()
            .Include(rf => rf.Reservation)
                .ThenInclude(r => r!.Experience)
                    .ThenInclude(e => e!.Company);

        if (operatorId.HasValue)
        {
            query = query.Where(rf => rf.Reservation != null
                && rf.Reservation.Experience != null
                && rf.Reservation.Experience.Company != null
                && rf.Reservation.Experience.Company.OperatorId == operatorId.Value);
        }

        return await query.OrderByDescending(rf => rf.RequestedAt).ToListAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await operation(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
