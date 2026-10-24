using FluentValidation;

using RutaSV.Application.Identity.DTO;

namespace RutaSV.Application.Identity.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="RegisterRequestValidator"/> y define las reglas de validación para el registro de usuarios.
    /// </summary>
    public RegisterRequestValidator()
    {
        // Validación del correo electrónico: obligatorio, máximo 150 caracteres y formato válido
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .MaximumLength(150).WithMessage("El correo electrónico no puede exceder 150 caracteres.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.");

        // La contraseña es obligatoria, debe tener entre 8 y 100 caracteres, al menos una letra mayúscula, una letra minúscula y un número
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un número.");

        // El nombre es obligatorio y con máximo 80 caracteres
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(80).WithMessage("El nombre no puede exceder 80 caracteres.");

        // El apellido es obligatorio y con máximo 80 caracteres
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(80).WithMessage("El apellido no puede exceder 80 caracteres.");

        // El teléfono es opcional, pero si se proporciona, no puede exceder 20 caracteres
        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("El teléfono no puede exceder 20 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
