using RutaSV.Domain.Common;

namespace RutaSV.Domain.Catalog.Exception;

public sealed class ExperienceScheduleNotFoundException(Guid scheduleId)
    : DomainException($"No se encontró el horario con identificador '{scheduleId}'.");
