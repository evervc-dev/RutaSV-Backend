using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RutaSV.Application.Identity;
using RutaSV.Domain.Identity;

namespace RutaSV.Infrastructure.Security;

public sealed class JwtProvider(IConfiguration configuration) : IJwtProvider
{
    public string GenerateToken(User user)
    {
        string secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("La clave secreta 'Jwt:SecretKey' no está configurada.");

        string issuer = configuration["Jwt:Issuer"] ?? "RutaSV.Api";
        string audience = configuration["Jwt:Audience"] ?? "RutaSV.Client";
        int expirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out int parsedMinutes)
            ? parsedMinutes
            : 60;

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(secretKey));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

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

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
