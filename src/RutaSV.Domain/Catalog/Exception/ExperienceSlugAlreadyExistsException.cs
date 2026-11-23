using RutaSV.Domain.Common;

namespace RutaSV.Domain.Catalog.Exception;

public sealed class ExperienceSlugAlreadyExistsException(string slug)
    : DomainException($"Ya existe una experiencia registrada con el slug '{slug}'.");
