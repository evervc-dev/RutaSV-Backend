using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Identity;

public interface IUserTokenRepository
{
    /// <summary>
    /// Obtiene un token de usuario por su hash y tipo (para cuando el usuario desea recuperar su token).
    /// </summary>
    /// <param name="tokenHash">Token hash enviado por el cliente</param>
    /// <param name="type">Tipo de token</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>El token de usuario si se encuentra, null en caso contrario</returns>
    Task<UserToken?> GetByTokenHashAsync(string tokenHash, TokenType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega un nuevo token de usuario a la base de datos.
    /// </summary>
    /// <param name="userToken">El token de usuario a agregar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task AddAsync(UserToken userToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un token de usuario existente en la base de datos.
    /// </summary>
    /// <param name="userToken">El token de usuario a actualizar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns></returns>
    Task UpdateAsync(UserToken userToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda los cambios realizados en la base de datos de manera asíncrona.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
