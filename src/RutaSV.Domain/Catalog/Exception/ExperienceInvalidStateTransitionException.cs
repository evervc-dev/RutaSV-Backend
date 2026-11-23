using RutaSV.Domain.Common;

namespace RutaSV.Domain.Catalog.Exception;

public sealed class ExperienceInvalidStateTransitionException(string message) : DomainException(message);
