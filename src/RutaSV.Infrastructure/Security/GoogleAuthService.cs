using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using RutaSV.Application.Identity;
using RutaSV.Domain.Identity;

namespace RutaSV.Infrastructure.Security;

public sealed class GoogleAuthService(IConfiguration configuration) : IGoogleAuthService
{
    public async Task<GoogleUserPayload> ValidateIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? clientId = configuration["Google:ClientId"];

        GoogleJsonWebSignature.ValidationSettings settings = new();
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            settings.Audience = [clientId];
        }

        try
        {
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new InvalidCredentialsException("El token de Google no contiene un correo electrónico válido.");
            }

            string firstName = !string.IsNullOrWhiteSpace(payload.GivenName)
                ? payload.GivenName
                : payload.Name ?? "Usuario";

            string lastName = !string.IsNullOrWhiteSpace(payload.FamilyName)
                ? payload.FamilyName
                : string.Empty;

            return new GoogleUserPayload(
                payload.Subject,
                payload.Email,
                firstName,
                lastName,
                payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            throw new InvalidCredentialsException($"El token de Google es inválido o ha expirado: {ex.Message}");
        }
    }
}
