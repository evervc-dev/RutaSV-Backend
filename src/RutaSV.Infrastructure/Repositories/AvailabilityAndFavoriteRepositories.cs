using Microsoft.EntityFrameworkCore;

using RutaSV.Application.Catalog.Repository;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Model;
using RutaSV.Infrastructure.Persistence;

namespace RutaSV.Infrastructure.Repositories;

public sealed class AvailabilityRepository(ApplicationDbContext dbContext) : IAvailabilityRepository
{
    public async Task<Availability?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Availabilities
            .Include(a => a.Experience)
                .ThenInclude(e => e!.Company)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Availability?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // RN-03: bloqueo pesimista FOR UPDATE en PostgreSQL para evitar sobreventa
        return await dbContext.Availabilities
            .FromSqlInterpolated($"SELECT * FROM availability WHERE id = {id} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Availability>> GetByExperienceAsync(
        Guid experienceId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        bool includeUnavailable = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Availability> query = dbContext.Availabilities
            .AsNoTracking()
            .Where(a => a.ExperienceId == experienceId);

        if (!includeUnavailable)
        {
            query = query.Where(a => a.Status == AvailabilityStatus.ABIERTA || a.Status == AvailabilityStatus.AGOTADA);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Date <= toDate.Value);
        }

        return await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Availability>> GetByOperatorAsync(
        Guid? operatorId,
        Guid? experienceId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Availability> query = dbContext.Availabilities
            .AsNoTracking()
            .Include(a => a.Experience)
                .ThenInclude(e => e!.Company)
            .Where(a => a.Experience != null && a.Experience.DeletedAt == null);

        if (operatorId.HasValue)
        {
            query = query.Where(a => a.Experience!.Company != null && a.Experience.Company.OperatorId == operatorId.Value);
        }

        if (experienceId.HasValue)
        {
            query = query.Where(a => a.ExperienceId == experienceId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Date <= toDate.Value);
        }

        return await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsSlotAsync(
        Guid experienceId,
        DateOnly date,
        TimeOnly startTime,
        Guid? excludeAvailabilityId = null,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Availabilities.AnyAsync(a =>
            a.ExperienceId == experienceId
            && a.Date == date
            && a.StartTime == startTime
            && (!excludeAvailabilityId.HasValue || a.Id != excludeAvailabilityId.Value),
            cancellationToken);
    }

    public Task AddAsync(Availability availability, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Entry(availability).State = EntityState.Added;
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Availability> availabilities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<Availability> snapshot = availabilities.ToList();
        foreach (Availability availability in snapshot)
        {
            dbContext.Entry(availability).State = EntityState.Added;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Availability availability, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Entry(availability).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ExperienceSchedule>> GetSchedulesByExperienceAsync(
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ExperienceSchedules
            .AsNoTracking()
            .Where(s => s.ExperienceId == experienceId)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExperienceSchedule?> GetScheduleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.ExperienceSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddScheduleAsync(ExperienceSchedule schedule, CancellationToken cancellationToken = default)
    {
        _ = await dbContext.ExperienceSchedules.AddAsync(schedule, cancellationToken);
    }

    public Task DeleteScheduleAsync(ExperienceSchedule schedule, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = dbContext.ExperienceSchedules.Remove(schedule);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class FavoriteRepository(ApplicationDbContext dbContext) : IFavoriteRepository
{
    public async Task<IReadOnlyList<Favorite>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Favorites
            .AsNoTracking()
            .Include(f => f.Experience)
                .ThenInclude(e => e!.Company)
            .Include(f => f.Experience)
                .ThenInclude(e => e!.Municipality)
                    .ThenInclude(m => m!.Department)
            .Include(f => f.Experience)
                .ThenInclude(e => e!.Categories)
                    .ThenInclude(ec => ec.Category)
            .Include(f => f.Experience)
                .ThenInclude(e => e!.Images)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetExperienceIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.ExperienceId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default)
    {
        return dbContext.Favorites.AnyAsync(
            f => f.UserId == userId && f.ExperienceId == experienceId,
            cancellationToken);
    }

    public Task AddAsync(Favorite favorite, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Entry(favorite).State = EntityState.Added;
        return Task.CompletedTask;
    }

    public async Task RemoveAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default)
    {
        Favorite? existing = await dbContext.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ExperienceId == experienceId, cancellationToken);

        if (existing is not null)
        {
            _ = dbContext.Favorites.Remove(existing);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
