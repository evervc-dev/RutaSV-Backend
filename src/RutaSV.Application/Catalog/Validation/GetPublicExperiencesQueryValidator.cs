using FluentValidation;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Domain.Catalog.Enum;

namespace RutaSV.Application.Catalog.Validation;

public sealed class GetPublicExperiencesQueryValidator : AbstractValidator<GetPublicExperiencesQuery>
{
    private static readonly string[] AllowedSortValues = ["recent", "price_asc", "price_desc", "rating"];

    public GetPublicExperiencesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("El número de página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio mínimo no puede ser negativo.")
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio máximo no puede ser negativo.")
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MaxPrice.Value >= x.MinPrice.Value)
            .WithMessage("El precio máximo debe ser mayor o igual al precio mínimo.");

        RuleFor(x => x.MinRating)
            .InclusiveBetween(0m, 5m).WithMessage("La calificación mínima debe estar entre 0 y 5.")
            .When(x => x.MinRating.HasValue);

        RuleFor(x => x.Difficulty)
            .Must(d => Enum.TryParse<DifficultyLevel>(d!.Trim(), ignoreCase: true, out _))
            .WithMessage("El filtro de dificultad no es válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Difficulty));

        RuleFor(x => x.SortBy)
            .Must(s => AllowedSortValues.Contains(s!.Trim().ToLowerInvariant()))
            .WithMessage("El criterio de ordenamiento no es válido. Valores permitidos: recent, price_asc, price_desc, rating.")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
    }
}
