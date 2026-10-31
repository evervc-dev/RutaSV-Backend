using FluentValidation;

using RutaSV.Application.Identity.DTO;
using RutaSV.Domain.Identity.Enum;

namespace RutaSV.Application.Identity.Validation;

public sealed class GetOperatorRequestsQueryValidator : AbstractValidator<GetOperatorRequestsQuery>
{
    public GetOperatorRequestsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");

        RuleFor(x => x.Status)
            .Must(BeValidStatus)
            .WithMessage("El estado de solicitud proporcionado no es válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));
    }

    private static bool BeValidStatus(string? status)
    {
        return !string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<OperatorRequestStatus>(status.Trim(), ignoreCase: true, out _);
    }
}
