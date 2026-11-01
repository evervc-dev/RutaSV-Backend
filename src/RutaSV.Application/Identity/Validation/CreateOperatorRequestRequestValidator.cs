using FluentValidation;

using RutaSV.Application.Identity.DTO;
using RutaSV.Domain.Identity.Enum;

namespace RutaSV.Application.Identity.Validation;

public sealed class CreateOperatorRequestRequestValidator : AbstractValidator<CreateOperatorRequestRequest>
{
    public CreateOperatorRequestRequestValidator()
    {
        RuleFor(x => x.Company)
            .NotNull().WithMessage("Los datos de la empresa son obligatorios.");

        When(x => x.Company is not null, () =>
        {
            RuleFor(x => x.Company.TradeName)
                .NotEmpty().WithMessage("El nombre comercial de la empresa es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre comercial no puede exceder 150 caracteres.");

            RuleFor(x => x.Company.LegalName)
                .MaximumLength(200).WithMessage("El nombre legal no puede exceder 200 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Company.LegalName));

            RuleFor(x => x.Company.ContactEmail)
                .MaximumLength(150).WithMessage("El correo de contacto no puede exceder 150 caracteres.")
                .EmailAddress().WithMessage("El formato del correo de contacto no es válido.")
                .When(x => !string.IsNullOrWhiteSpace(x.Company.ContactEmail));

            RuleFor(x => x.Company.ContactPhone)
                .MaximumLength(20).WithMessage("El teléfono de contacto no puede exceder 20 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Company.ContactPhone));

            RuleFor(x => x.Company.Whatsapp)
                .MaximumLength(20).WithMessage("El número de WhatsApp no puede exceder 20 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Company.Whatsapp));

            RuleFor(x => x.Company.Website)
                .MaximumLength(300).WithMessage("El sitio web no puede exceder 300 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Company.Website));

            RuleFor(x => x.Company.LogoUrl)
                .MaximumLength(500).WithMessage("La URL del logo no puede exceder 500 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Company.LogoUrl));

            RuleFor(x => x.Company.MunicipalityId)
                .GreaterThan(0).WithMessage("El identificador del municipio debe ser mayor que 0.")
                .When(x => x.Company.MunicipalityId.HasValue);

            RuleFor(x => x.Company.Address)
                .MaximumLength(300).WithMessage("La dirección no puede exceder 300 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Company.Address));
        });

        RuleFor(x => x.Message)
            .MaximumLength(500).WithMessage("El mensaje no puede exceder 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Message));

        RuleFor(x => x.Documents)
            .NotEmpty().WithMessage("Debe adjuntar al menos un documento de respaldo.");

        RuleForEach(x => x.Documents).ChildRules(document =>
        {
            document.RuleFor(d => d.Type)
                .NotEmpty().WithMessage("El tipo de documento es obligatorio.")
                .Must(BeValidDocumentType)
                .WithMessage("El tipo de documento proporcionado no es válido.");

            document.RuleFor(d => d.ObjectKey)
                .MaximumLength(300).WithMessage("La clave del objeto no puede exceder 300 caracteres.")
                .When(d => !string.IsNullOrWhiteSpace(d.ObjectKey));

            document.RuleFor(d => d.Url)
                .MaximumLength(500).WithMessage("La URL del documento no puede exceder 500 caracteres.")
                .When(d => !string.IsNullOrWhiteSpace(d.Url));

            document.RuleFor(d => d)
                .Must(d => !string.IsNullOrWhiteSpace(d.ObjectKey) || !string.IsNullOrWhiteSpace(d.Url))
                .WithMessage("Cada documento debe especificar al menos un 'ObjectKey' o una 'Url'.");
        });
    }

    private static bool BeValidDocumentType(string? type)
    {
        return !string.IsNullOrWhiteSpace(type)
            && Enum.TryParse<OperatorRequestDocumentType>(type.Trim(), ignoreCase: true, out _);
    }
}
