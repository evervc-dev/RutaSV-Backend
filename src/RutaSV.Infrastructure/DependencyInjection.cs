using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RutaSV.Application.Identity;
using RutaSV.Infrastructure.Email;
using RutaSV.Infrastructure.Persistence;
using RutaSV.Infrastructure.Repositories;
using RutaSV.Infrastructure.Security;

namespace RutaSV.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();

        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        string? jwtSecret = configuration["Jwt:SecretKey"];
        if (!string.IsNullOrWhiteSpace(jwtSecret))
        {
            string issuer = configuration["Jwt:Issuer"] ?? "RutaSV.Api";
            string audience = configuration["Jwt:Audience"] ?? "RutaSV.Client";

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
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

        return services;
    }
}
