using Microsoft.EntityFrameworkCore;
using RutaSV.Application.Identity.Repository;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;
using RutaSV.Infrastructure.Persistence;

namespace RutaSV.Infrastructure.Repositories;

public sealed class UserTokenRepository(ApplicationDbContext dbContext) : IUserTokenRepository
{
    // Sirve cuando se necesita validar un token de usuario, por ejemplo, durante el proceso de verificación de correo electrónico o restablecimiento de contraseña.
    public async Task<UserToken?> GetByTokenHashAsync(
        string tokenHash,
        TokenType type,
        CancellationToken cancellationToken = default)
    {
        // Con FirstOrDefaultAsync busca un token de usuario por su hash y tipo, asegurándose de que esté disponible en el resultado.
        return await dbContext.UserTokens
            .Include(t => t.User) // Incluye la entidad relacionada User para que esté disponible en el resultado.
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.Type == type, // Filtra por hash y tipo de token
                cancellationToken);
    }

    public async Task AddAsync(UserToken userToken, CancellationToken cancellationToken = default)
    {
        // Con AddAsync agrega un nuevo token de usuario al contexto de base de datos.
        _ = await dbContext.UserTokens.AddAsync(userToken, cancellationToken);
    }

    public Task UpdateAsync(UserToken userToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); // Lanza una excepción si la operación ha sido cancelada.
        _ = dbContext.UserTokens.Update(userToken); // Actualiza un token de usuario existente en el contexto de base de datos.
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Con SaveChangesAsync guarda los cambios realizados en el contexto de base de datos.
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
