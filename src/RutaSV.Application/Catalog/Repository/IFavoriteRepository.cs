using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Application.Catalog.Repository;

public interface IFavoriteRepository
{
    Task<IReadOnlyList<Favorite>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetExperienceIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default);
    Task AddAsync(Favorite favorite, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
