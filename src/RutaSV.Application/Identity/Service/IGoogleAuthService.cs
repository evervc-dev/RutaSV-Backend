using RutaSV.Application.Identity.DTO;

namespace RutaSV.Application.Identity.Service;

public interface IGoogleAuthService
{
    /// <summary>
    /// Valida el token de identificación de Google y devuelve la información del usuario autenticado.
    /// </summary>
    /// <param name="idToken">Token de identificación de Google.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Información del usuario autenticado.</returns>
    Task<GoogleUserPayload> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
