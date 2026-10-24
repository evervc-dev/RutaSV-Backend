using Microsoft.EntityFrameworkCore;

using RutaSV.Application.Identity.Repository;
using RutaSV.Domain.Identity.Model;
using RutaSV.Infrastructure.Persistence;

namespace RutaSV.Infrastructure.Repositories;

// Implementación del repositorio de usuarios utilizando Entity Framework Core, ApllicationDbContext (es decir, el contexto de base de datos) y el patrón de repositorio.
public sealed class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Con FirstOrDefaultAsync busca un usuario por su Id, asegurándose de que no esté marcado como eliminado (DeletedAt == null).
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant(); // Normaliza el correo electrónico para comparaciones consistentes.
        // Con FirstOrDefaultAsync busca un usuario por su correo electrónico, asegurándose de que no esté marcado como eliminado (DeletedAt == null).
        return await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant(); // Normaliza el correo electrónico para comparaciones consistentes.
        // Con AnyAsync verifica si existe algún usuario con el correo electrónico proporcionado
        return await dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // Con AddAsync agrega un nuevo usuario al contexto de base de datos.
        _ = await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); // Lanza una excepción si la operación ha sido cancelada.
        _ = dbContext.Users.Update(user); // Actualiza un usuario existente en el contexto de base de datos.
        return Task.CompletedTask; // Devuelve una tarea completada, ya que la actualización se realiza en memoria y no requiere una operación asincrónica.
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Con SaveChangesAsync guarda los cambios realizados en el contexto de base de datos.
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
