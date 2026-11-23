using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Identity.Repository;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Company>> GetByOperatorIdAsync(Guid operatorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Company>> GetApprovedCompaniesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
