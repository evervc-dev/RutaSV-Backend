using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Service;
using RutaSV.Domain.Identity.Exception;

namespace RutaSV.Infrastructure.Security;

public sealed class GoogleAuthService(IConfiguration configuration) : IGoogleAuthService
{
    public async Task<GoogleUserPayload> ValidateIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); // Lanza una excepción si la operación ha sido cancelada.

        string? clientId = configuration["Google:ClientId"]; // Obtiene el Client ID de Google desde la configuración.

        // Configura los ajustes de validación del token de Google, incluyendo la audiencia (Client ID) si está disponible.
        GoogleJsonWebSignature.ValidationSettings settings = new();
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            settings.Audience = [clientId];
        }

        try
        {
            // Valida el token de Google y obtiene la carga útil (payload) del token.
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            // Verifica que el token contenga un correo electrónico válido; si no, lanza una excepción.
            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new InvalidCredentialsException("El token de Google no contiene un correo electrónico válido.");
            }

            // Si el nombre o apellido no están disponibles en la carga útil, se asignan valores predeterminados.
            string firstName = !string.IsNullOrWhiteSpace(payload.GivenName)
                ? payload.GivenName
                : payload.Name ?? "Usuario";

            string lastName = !string.IsNullOrWhiteSpace(payload.FamilyName)
                ? payload.FamilyName
                : string.Empty;

            // Devuelve un objeto GoogleUserPayload con la información del usuario obtenida del token de Google.
            return new GoogleUserPayload(
                payload.Subject,
                payload.Email,
                firstName,
                lastName,
                payload.Picture
            );
        }
        catch (InvalidJwtException ex)
        {
            // Lanza una excepción personalizada si el token de Google es inválido o ha expirado
            throw new InvalidCredentialsException($"El token de Google es inválido o ha expirado: {ex.Message}");
        }
    }
}
