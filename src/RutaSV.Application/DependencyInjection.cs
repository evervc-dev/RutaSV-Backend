using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using RutaSV.Application.Bookings.Service;
using RutaSV.Application.Catalog.Service;
using RutaSV.Application.Identity.Service;

namespace RutaSV.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Agrega los servicios de la capa de aplicación al contenedor de dependencias.
    /// </summary>
    /// <param name="services">El contenedor de dependencias.</param>
    /// <returns>El contenedor de dependencias actualizado.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly); // Agrega los validadores de FluentValidation desde el ensamblado actual
        services.AddScoped<IAuthService, AuthService>(); // Agrega el servicio de autenticación a la colección de servicios con un tiempo de vida Scoped
        services.AddScoped<IOperatorRequestService, OperatorRequestService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IExperienceService, ExperienceService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IReservationIncidentCoordinator>(sp => sp.GetRequiredService<IReservationService>());

        return services;
    }
}
