using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RutaSV.Application.Identity.Service;

namespace RutaSV.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
