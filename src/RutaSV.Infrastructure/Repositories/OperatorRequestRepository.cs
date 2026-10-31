using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using RutaSV.Application.Identity.Repository;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;
using RutaSV.Infrastructure.Persistence;

namespace RutaSV.Infrastructure.Repositories;

public sealed class OperatorRequestRepository(ApplicationDbContext dbContext) : IOperatorRequestRepository
{
    public async Task<OperatorRequest?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OperatorRequests
            .Include(r => r.User)
            .Include(r => r.Company)
            .Include(r => r.Documents.Where(d => d.DeletedAt == null))
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<OperatorRequest?> GetLatestByUserIdWithDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OperatorRequests
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Company)
            .Include(r => r.Documents.Where(d => d.DeletedAt == null))
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasPendingRequestByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OperatorRequests
            .AnyAsync(
                r => r.UserId == userId && r.Status == OperatorRequestStatus.PENDIENTE,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<OperatorRequest> Items, int TotalRecords)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        OperatorRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OperatorRequest> query = dbContext.OperatorRequests
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Company)
            .Include(r => r.Documents.Where(d => d.DeletedAt == null));

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        int totalRecords = await query.CountAsync(cancellationToken);

        List<OperatorRequest> items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalRecords);
    }

    public async Task AddAsync(
        OperatorRequest operatorRequest,
        CancellationToken cancellationToken = default)
    {
        _ = await dbContext.OperatorRequests.AddAsync(operatorRequest, cancellationToken);
    }

    public async Task AddDocumentsAsync(
        IEnumerable<OperatorRequestDocument> documents,
        CancellationToken cancellationToken = default)
    {
        await dbContext.OperatorRequestDocuments.AddRangeAsync(documents, cancellationToken);
    }

    public async Task ReplaceDocumentsAsync(
        Guid requestId,
        IEnumerable<OperatorRequestDocument> documents,
        CancellationToken cancellationToken = default)
    {
        List<OperatorRequestDocument> existing = await dbContext.OperatorRequestDocuments
            .Where(d => d.RequestId == requestId && d.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            dbContext.OperatorRequestDocuments.RemoveRange(existing);
        }

        await dbContext.OperatorRequestDocuments.AddRangeAsync(documents, cancellationToken);
    }

    public Task UpdateAsync(
        OperatorRequest operatorRequest,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = dbContext.OperatorRequests.Update(operatorRequest);
        return Task.CompletedTask;
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
