using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Identity.Repository;

public interface IOperatorRequestRepository
{
    Task<OperatorRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperatorRequest?> GetLatestByUserIdWithDetailsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingRequestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<OperatorRequest> Items, int TotalRecords)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        OperatorRequestStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(OperatorRequest operatorRequest, CancellationToken cancellationToken = default);
    Task AddDocumentsAsync(IEnumerable<OperatorRequestDocument> documents, CancellationToken cancellationToken = default);
    Task ReplaceDocumentsAsync(Guid requestId, IEnumerable<OperatorRequestDocument> documents, CancellationToken cancellationToken = default);
    Task UpdateAsync(OperatorRequest operatorRequest, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
