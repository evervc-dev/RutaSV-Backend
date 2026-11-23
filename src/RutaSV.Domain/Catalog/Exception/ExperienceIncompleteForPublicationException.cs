using RutaSV.Domain.Common;

namespace RutaSV.Domain.Catalog.Exception;

public sealed class ExperienceIncompleteForPublicationException(IReadOnlyList<string> missingFields)
    : DomainException("La experiencia no cumple con todos los requisitos obligatorios para ser publicada.")
{
    public IReadOnlyList<string> MissingFields { get; } = missingFields;
}
