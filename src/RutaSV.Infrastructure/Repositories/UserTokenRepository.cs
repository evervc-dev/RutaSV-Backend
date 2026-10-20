using Microsoft.EntityFrameworkCore;
using RutaSV.Application.Identity;
using RutaSV.Domain.Identity;
using RutaSV.Infrastructure.Persistence;

namespace RutaSV.Infrastructure.Repositories;

public sealed class UserTokenRepository(ApplicationDbContext dbContext) : IUserTokenRepository
{
    public async Task<UserToken?> GetByTokenHashAsync(
        string tokenHash,
        TokenType type,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.Type == type,
                cancellationToken);
    }

    public async Task AddAsync(UserToken userToken, CancellationToken cancellationToken = default)
    {
        _ = await dbContext.UserTokens.AddAsync(userToken, cancellationToken);
    }

    public Task UpdateAsync(UserToken userToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = dbContext.UserTokens.Update(userToken);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
