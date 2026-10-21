using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RutaSV.Application.Identity;
using RutaSV.Application.Identity.Repository;
using RutaSV.Application.Identity.Security;
using RutaSV.Application.Identity.Service;
using RutaSV.Infrastructure.Email;
using RutaSV.Infrastructure.Persistence;
using RutaSV.Infrastructure.Repositories;
using RutaSV.Infrastructure.Security;

namespace RutaSV.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Agrega los servicios de infraestructura al contenedor de servicios.
    /// </summary>
    /// <param name="services">El contenedor de servicios.</param>
    /// <param name="configuration">La configuración de la aplicación.</param>
    /// <returns>El contenedor de servicios actualizado.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configura la cadena de conexión a la base de datos desde la configuración.
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        // Configura el contexto de la base de datos utilizando Npgsql (PostgreSQL) y la cadena de conexión proporcionada.
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Agrega los repositorios al contenedor de servicios con un tiempo de vida "scoped" (una instancia por solicitud).
        // Scoped  permite que cada solicitud HTTP tenga su propia instancia de los repositorios, lo que es útil para mantener el estado de la base de datos durante la solicitud.
        // Y que cuando se guarde en uno de los repositorios, se pueda reflejar en los demás repositorios que estén usando la misma instancia del contexto de base de datos.
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();

        // Agrega los servicios de seguridad y autenticación al contenedor de servicios.
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        string? jwtSecret = configuration["Jwt:SecretKey"]; // Obtiene la clave secreta para la firma de tokens JWT desde la configuración.

        // Si la clave secreta no está vacía, configura la autenticación JWT utilizando la clave secreta y los parámetros de validación del token.
        if (!string.IsNullOrWhiteSpace(jwtSecret))
        {
            string issuer = configuration["Jwt:Issuer"] ?? "RutaSV.Api"; // Valor predeterminado del emisor del token JWT si no se proporciona en la configuración.
            string audience = configuration["Jwt:Audience"] ?? "RutaSV.Client"; // Valor predeterminado de el destinatario del token JWT si no se proporciona en la configuración.

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Configura los parámetros de validación del token JWT, incluyendo la validación del emisor, destinatario, tiempo de vida y clave de firma.
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                    };
                });
        }

        return services; // Devuelve el contenedor de servicios para permitir la encadenación de llamadas.
    }
}
