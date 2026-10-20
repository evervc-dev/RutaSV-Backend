using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Identity;

public interface IUserTokenRepository
{
    Task<UserToken?> GetByTokenHashAsync(string tokenHash, TokenType type, CancellationToken cancellationToken = default);
    Task AddAsync(UserToken userToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserToken userToken, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
