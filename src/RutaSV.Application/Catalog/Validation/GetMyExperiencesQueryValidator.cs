using FluentValidation;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Domain.Catalog.Enum;

namespace RutaSV.Application.Catalog.Validation;

public sealed class GetMyExperiencesQueryValidator : AbstractValidator<GetMyExperiencesQuery>
{
    public GetMyExperiencesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");

        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<ExperienceStatus>(s!.Trim(), ignoreCase: true, out _))
            .WithMessage("El estado de experiencia proporcionado no es válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));
    }
}
