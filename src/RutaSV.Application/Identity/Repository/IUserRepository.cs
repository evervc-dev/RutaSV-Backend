using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Identity.Repository;

//  Add y Update permiten modificar el estado de la entidad User, pero no persisten los cambios en la base de datos. 
// Para guardar los cambios en la base de datos, se debe llamar al método SaveChangesAsync() después de realizar las operaciones de Add o Update.
public interface IUserRepository
{
    /// <summary>
    /// Obtiene un usuario por su identificador único (ID) de tipo Guid.
    /// </summary>
    /// <param name="id">ID del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>El usuario encontrado o null si no se encuentra</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un usuario por su correo electrónico.
    /// </summary>
    /// <param name="email">Correo electrónico del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>El usuario encontrado o null si no se encuentra</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si un usuario con el correo electrónico especificado ya existe en el sistema.
    /// </summary>
    /// <param name="email">Correo electrónico del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>true si el usuario existe, false en caso contrario</returns>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega un nuevo usuario al repositorio sin persistir los cambios en la base de datos.
    /// </summary>
    /// <param name="user">Usuario a agregar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns></returns>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un usuario existente en el repositorio sin persistir los cambios en la base de datos.
    /// </summary>
    /// <param name="user">Usuario a actualizar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns></returns>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste los cambios realizados en el repositorio en la base de datos (Add y Update no persisten los cambios automáticamente).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
