using FluentValidation;

using RutaSV.Application.Identity.DTO;

namespace RutaSV.Application.Identity.Validation;

public sealed class ResendVerificationEmailRequestValidator : AbstractValidator<ResendVerificationEmailRequest>
{
    public ResendVerificationEmailRequestValidator()
    {
        // Validación para el campo Email: debe ser obligatorio y tener un formato válido
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");
    }
}
