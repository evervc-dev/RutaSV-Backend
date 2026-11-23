using RutaSV.Domain.Common;

namespace RutaSV.Domain.Catalog.Exception;

public sealed class ExperienceAccessDeniedException(string message) : DomainException(message);
