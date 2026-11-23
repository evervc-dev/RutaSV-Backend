using RutaSV.Domain.Common;

namespace RutaSV.Domain.Catalog.Exception;

public sealed class ExperienceNotFoundException : DomainException
{
    public ExperienceNotFoundException(Guid experienceId)
        : base($"No se encontró la experiencia con identificador '{experienceId}'.")
    {
    }

    public ExperienceNotFoundException(string slug)
        : base($"No se encontró la experiencia '{slug}'.")
    {
    }
}
