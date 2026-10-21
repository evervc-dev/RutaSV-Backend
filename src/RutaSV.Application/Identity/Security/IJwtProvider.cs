using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Identity.Security;

public interface IJwtProvider
{
    /// <summary>
    /// Genera un token JWT para el usuario especificado.
    /// </summary>
    /// <param name="user">El usuario para el cual se generará el token.</param>
    /// <returns>El token JWT generado.</returns>
    string GenerateToken(User user);
}
