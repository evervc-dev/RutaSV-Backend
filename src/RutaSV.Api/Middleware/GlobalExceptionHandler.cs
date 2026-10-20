using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RutaSV.Domain.Common;
using RutaSV.Domain.Identity;

namespace RutaSV.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Excepción capturada por GlobalExceptionHandler: {Message}", exception.Message);

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

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            Dictionary<string, string[]> errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
