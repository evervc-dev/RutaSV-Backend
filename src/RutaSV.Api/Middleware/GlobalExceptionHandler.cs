using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using RutaSV.Domain.Common;
using RutaSV.Domain.Identity.Exception;

namespace RutaSV.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Intenta manejar la excepción capturada y devuelve un resultado adecuado al cliente.
    /// </summary>
    /// <param name="httpContext">Contexto de la solicitud HTTP</param>
    /// <param name="exception">La excepción capturada</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si la excepción fue manejada, false en caso contrario</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Registrar la excepción para fines de diagnóstico
        logger.LogError(exception, "Excepción capturada por GlobalExceptionHandler: {Message}", exception.Message);

        // Determinar el código de estado HTTP y el mensaje de error según el tipo de excepción
        (int statusCode, string title, string detail) = exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Error de validación",
                "Uno o más campos contienen errores de validación."),
            InvalidCredentialsException => (
                StatusCodes.Status401Unauthorized,
                "Credenciales inválidas",
                exception.Message),
            UserAlreadyExistsException => (
                StatusCodes.Status409Conflict,
                "El usuario ya existe",
                exception.Message),
            InvalidTokenException => (
                StatusCodes.Status400BadRequest,
                "Token inválido",
                exception.Message),
            DomainException => (
                StatusCodes.Status400BadRequest,
                "Violación de regla de dominio",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                "Ocurrió un error inesperado al procesar la solicitud.")
        };

        // Crear un objeto ProblemDetails para proporcionar información estructurada sobre el error
        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // Si la excepción es de tipo ValidationException (errores generados en la validación de datos), agregar los errores de validación al objeto ProblemDetails
        if (exception is ValidationException validationException)
        {
            Dictionary<string, string[]> errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            problemDetails.Extensions["errors"] = errors;
        }

        // Establecer el código de estado de la respuesta y escribir los detalles del error en formato JSON
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
