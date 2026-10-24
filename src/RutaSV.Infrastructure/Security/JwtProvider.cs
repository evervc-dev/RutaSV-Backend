using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using RutaSV.Application.Identity.Security;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Infrastructure.Security;

public sealed class JwtProvider(IConfiguration configuration) : IJwtProvider
{
    public string GenerateToken(User user)
    {
        // Obtiene la clave secreta, el emisor, el público y la duración del token desde la configuración
        string secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("La clave secreta 'Jwt:SecretKey' no está configurada.");

        // Obtiene el emisor y el público del token desde la configuración, con valores predeterminados si no están configurados
        string issuer = configuration["Jwt:Issuer"] ?? "RutaSV.Api";
        string audience = configuration["Jwt:Audience"] ?? "RutaSV.Client";

        // Obtiene la duración del token en minutos desde la configuración, con un valor predeterminado de 60 minutos si no está configurado o no es un número válido
        int expirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out int parsedMinutes)
            ? parsedMinutes
            : 60;

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(secretKey)); // Crea una clave de seguridad simétrica a partir de la clave secreta
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256); // Crea las credenciales de firma utilizando la clave de seguridad y el algoritmo HMAC SHA256

        // Crea los claims (reclamaciones) que se incluirán en el token JWT, como el ID del usuario, correo electrónico, nombre, apellido, rol y si el correo electrónico está verificado
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        // Crea el token JWT utilizando los claims, el emisor, el público, la fecha de expiración y las credenciales de firma
        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token); // Convierte el token JWT a una cadena y la retorna
    }
}
