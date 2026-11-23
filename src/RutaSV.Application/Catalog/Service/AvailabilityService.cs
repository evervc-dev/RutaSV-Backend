using System.Globalization;

using FluentValidation;

using RutaSV.Application.Bookings.Service;
using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Repository;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Exception;
using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Application.Catalog.Service;

public interface IAvailabilityService
{
    Task<IReadOnlyList<AvailabilityDto>> GetByExperienceAsync(
        Guid experienceId,
        string? fromDate,
        string? toDate,
        bool includeUnavailable,
        Guid? currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailabilityDto>> GetMyAvailabilitiesAsync(
        Guid userId,
        bool isAdmin,
        Guid? experienceId = null,
        string? fromDate = null,
        string? toDate = null,
        CancellationToken cancellationToken = default);

    Task<AvailabilityDto> CreateAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CreateAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    Task<AvailabilityDto> UpdateAsync(
        Guid userId,
        bool isAdmin,
        Guid availabilityId,
        UpdateAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    Task<AvailabilityDto> CancelAsync(
        Guid userId,
        bool isAdmin,
        Guid availabilityId,
        CancelAvailabilityRequest? request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExperienceScheduleDto>> GetSchedulesAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CancellationToken cancellationToken = default);

    Task<ExperienceScheduleDto> CreateScheduleAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CreateExperienceScheduleRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteScheduleAsync(
        Guid userId,
        bool isAdmin,
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailabilityDto>> GenerateFromSchedulesAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        GenerateAvailabilitiesRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CreateAvailabilityRequestValidator : AbstractValidator<CreateAvailabilityRequest>
{
    public CreateAvailabilityRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha es obligatoria.")
            .Must(d => DateOnly.TryParse(d, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("La fecha debe tener formato YYYY-MM-DD.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("La hora de inicio es obligatoria.")
            .Must(t => TimeOnly.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("La hora de inicio debe tener formato HH:mm.");

        RuleFor(x => x.EndTime)
            .Must(t => TimeOnly.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("La hora de fin debe tener formato HH:mm.")
            .When(x => !string.IsNullOrWhiteSpace(x.EndTime));

        RuleFor(x => x.TotalCapacity)
            .GreaterThan(0).WithMessage("El cupo total debe ser mayor a 0.")
            .LessThanOrEqualTo(500).WithMessage("El cupo total no puede superar los 500 espacios.");

        RuleFor(x => x.SpecialPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio especial no puede ser negativo.")
            .When(x => x.SpecialPrice.HasValue);
    }
}

public sealed class UpdateAvailabilityRequestValidator : AbstractValidator<UpdateAvailabilityRequest>
{
    public UpdateAvailabilityRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha es obligatoria.")
            .Must(d => DateOnly.TryParse(d, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("La fecha debe tener formato YYYY-MM-DD.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("La hora de inicio es obligatoria.")
            .Must(t => TimeOnly.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("La hora de inicio debe tener formato HH:mm.");

        RuleFor(x => x.EndTime)
            .Must(t => TimeOnly.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("La hora de fin debe tener formato HH:mm.")
            .When(x => !string.IsNullOrWhiteSpace(x.EndTime));

        RuleFor(x => x.TotalCapacity)
            .GreaterThan(0).WithMessage("El cupo total debe ser mayor a 0.")
            .LessThanOrEqualTo(500).WithMessage("El cupo total no puede superar los 500 espacios.");

        RuleFor(x => x.SpecialPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio especial no puede ser negativo.")
            .When(x => x.SpecialPrice.HasValue);
    }
}

public sealed class AvailabilityService(
    IAvailabilityRepository availabilityRepository,
    IExperienceRepository experienceRepository,
    IValidator<CreateAvailabilityRequest> createValidator,
    IValidator<UpdateAvailabilityRequest> updateValidator,
    IReservationIncidentCoordinator? incidentCoordinator = null) : IAvailabilityService
{
    public async Task<IReadOnlyList<AvailabilityDto>> GetByExperienceAsync(
        Guid experienceId,
        string? fromDate,
        string? toDate,
        bool includeUnavailable,
        Guid? currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(experienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        bool isOwner = currentUserId.HasValue && experience.Company?.OperatorId == currentUserId.Value;
        bool allowUnavailable = includeUnavailable && (isOwner || isAdmin);

        DateOnly? parsedFrom = TryParseDate(fromDate);
        DateOnly? parsedTo = TryParseDate(toDate);

        if (!allowUnavailable && !parsedFrom.HasValue)
        {
            parsedFrom = BookingBusinessRules.GetElSalvadorToday();
        }

        IReadOnlyList<Availability> items = await availabilityRepository.GetByExperienceAsync(
            experienceId,
            parsedFrom,
            parsedTo,
            allowUnavailable,
            cancellationToken);

        return items.Select(a => MapToDto(a, experience)).ToList();
    }

    public async Task<IReadOnlyList<AvailabilityDto>> GetMyAvailabilitiesAsync(
        Guid userId,
        bool isAdmin,
        Guid? experienceId = null,
        string? fromDate = null,
        string? toDate = null,
        CancellationToken cancellationToken = default)
    {
        DateOnly? parsedFrom = TryParseDate(fromDate);
        DateOnly? parsedTo = TryParseDate(toDate);

        IReadOnlyList<Availability> items = await availabilityRepository.GetByOperatorAsync(
            isAdmin ? null : userId,
            experienceId,
            parsedFrom,
            parsedTo,
            cancellationToken);

        return items.Select(a => MapToDto(a, a.Experience)).ToList();
    }

    public async Task<AvailabilityDto> CreateAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CreateAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(experienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        EnsureCanManage(experience, userId, isAdmin);

        DateOnly date = DateOnly.Parse(request.Date, CultureInfo.InvariantCulture);
        TimeOnly startTime = TimeOnly.Parse(request.StartTime, CultureInfo.InvariantCulture);
        TimeOnly? endTime = string.IsNullOrWhiteSpace(request.EndTime)
            ? null
            : TimeOnly.Parse(request.EndTime, CultureInfo.InvariantCulture);

        if (endTime.HasValue && endTime.Value <= startTime)
        {
            throw new AvailabilityConflictException("La hora de fin debe ser posterior a la hora de inicio.");
        }

        bool duplicate = await availabilityRepository.ExistsSlotAsync(experienceId, date, startTime, null, cancellationToken);
        if (duplicate)
        {
            throw new AvailabilityConflictException(
                $"Ya existe una sesión programada el {date:yyyy-MM-dd} a las {startTime:HH:mm} para esta experiencia.");
        }

        DateTime now = DateTime.UtcNow;
        Availability availability = new()
        {
            Id = Guid.NewGuid(),
            ExperienceId = experienceId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            TotalCapacity = request.TotalCapacity,
            ReservedCapacity = 0,
            SpecialPrice = request.SpecialPrice.HasValue ? decimal.Round(request.SpecialPrice.Value, 2) : null,
            Status = AvailabilityStatus.ABIERTA,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now,
            Experience = experience
        };

        await availabilityRepository.AddAsync(availability, cancellationToken);
        await availabilityRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(availability, experience);
    }

    public async Task<AvailabilityDto> UpdateAsync(
        Guid userId,
        bool isAdmin,
        Guid availabilityId,
        UpdateAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        Availability availability = await availabilityRepository.GetByIdAsync(availabilityId, cancellationToken)
            ?? throw new AvailabilityNotFoundException(availabilityId);

        Experience experience = availability.Experience
            ?? await experienceRepository.GetByIdWithDetailsAsync(availability.ExperienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(availability.ExperienceId);

        EnsureCanManage(experience, userId, isAdmin);

        if (availability.Status == AvailabilityStatus.CANCELADA)
        {
            throw new AvailabilityConflictException("No se puede editar una sesión que ya fue cancelada.");
        }

        if (request.TotalCapacity < availability.ReservedCapacity)
        {
            throw new AvailabilityConflictException(
                $"El cupo total ({request.TotalCapacity}) no puede ser menor a los espacios ya reservados ({availability.ReservedCapacity}).");
        }

        DateOnly date = DateOnly.Parse(request.Date, CultureInfo.InvariantCulture);
        TimeOnly startTime = TimeOnly.Parse(request.StartTime, CultureInfo.InvariantCulture);
        TimeOnly? endTime = string.IsNullOrWhiteSpace(request.EndTime)
            ? null
            : TimeOnly.Parse(request.EndTime, CultureInfo.InvariantCulture);

        if (endTime.HasValue && endTime.Value <= startTime)
        {
            throw new AvailabilityConflictException("La hora de fin debe ser posterior a la hora de inicio.");
        }

        bool duplicate = await availabilityRepository.ExistsSlotAsync(
            availability.ExperienceId,
            date,
            startTime,
            availability.Id,
            cancellationToken);

        if (duplicate)
        {
            throw new AvailabilityConflictException(
                $"Ya existe otra sesión el {date:yyyy-MM-dd} a las {startTime:HH:mm} para esta experiencia.");
        }

        availability.Date = date;
        availability.StartTime = startTime;
        availability.EndTime = endTime;
        availability.TotalCapacity = request.TotalCapacity;
        availability.SpecialPrice = request.SpecialPrice.HasValue
            ? decimal.Round(request.SpecialPrice.Value, 2)
            : null;

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<AvailabilityStatus>(request.Status.Trim(), ignoreCase: true, out AvailabilityStatus requestedStatus))
        {
            if (requestedStatus == AvailabilityStatus.CANCELADA)
            {
                throw new AvailabilityConflictException("Para cancelar una sesión utiliza el endpoint de cancelación.");
            }

            availability.Status = requestedStatus == AvailabilityStatus.BLOQUEADA
                ? AvailabilityStatus.BLOQUEADA
                : availability.ReservedCapacity >= availability.TotalCapacity
                    ? AvailabilityStatus.AGOTADA
                    : AvailabilityStatus.ABIERTA;
        }
        else if (availability.Status != AvailabilityStatus.BLOQUEADA)
        {
            availability.Status = availability.ReservedCapacity >= availability.TotalCapacity
                ? AvailabilityStatus.AGOTADA
                : AvailabilityStatus.ABIERTA;
        }

        availability.Version++;
        availability.UpdatedAt = DateTime.UtcNow;

        await availabilityRepository.UpdateAsync(availability, cancellationToken);
        await availabilityRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(availability, experience);
    }

    public async Task<AvailabilityDto> CancelAsync(
        Guid userId,
        bool isAdmin,
        Guid availabilityId,
        CancelAvailabilityRequest? request,
        CancellationToken cancellationToken = default)
    {
        Availability availability = await availabilityRepository.GetByIdAsync(availabilityId, cancellationToken)
            ?? throw new AvailabilityNotFoundException(availabilityId);

        Experience experience = availability.Experience
            ?? await experienceRepository.GetByIdWithDetailsAsync(availability.ExperienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(availability.ExperienceId);

        EnsureCanManage(experience, userId, isAdmin);

        if (availability.Status == AvailabilityStatus.CANCELADA)
        {
            return MapToDto(availability, experience);
        }

        availability.Status = AvailabilityStatus.CANCELADA;
        availability.CancellationReason = request?.Reason?.Trim();
        availability.Version++;
        availability.UpdatedAt = DateTime.UtcNow;

        await availabilityRepository.UpdateAsync(availability, cancellationToken);
        await availabilityRepository.SaveChangesAsync(cancellationToken);

        if (incidentCoordinator is not null)
        {
            await incidentCoordinator.HandleAvailabilityCancelledAsync(
                availability,
                experience,
                userId,
                availability.CancellationReason,
                cancellationToken);
        }

        return MapToDto(availability, experience);
    }

    public async Task<IReadOnlyList<ExperienceScheduleDto>> GetSchedulesAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(experienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        EnsureCanManage(experience, userId, isAdmin);

        IReadOnlyList<ExperienceSchedule> schedules = await availabilityRepository.GetSchedulesByExperienceAsync(
            experienceId,
            cancellationToken);

        return schedules.Select(MapSchedule).ToList();
    }

    public async Task<ExperienceScheduleDto> CreateScheduleAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CreateExperienceScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(experienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        EnsureCanManage(experience, userId, isAdmin);

        if (request.DayOfWeek is < 0 or > 6)
        {
            throw new AvailabilityConflictException("El día de la semana debe estar entre 0 (Domingo) y 6 (Sábado).");
        }

        if (!TimeOnly.TryParse(request.StartTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly startTime))
        {
            throw new AvailabilityConflictException("La hora de inicio debe tener formato HH:mm.");
        }

        TimeOnly? endTime = null;
        if (!string.IsNullOrWhiteSpace(request.EndTime))
        {
            if (!TimeOnly.TryParse(request.EndTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedEnd))
            {
                throw new AvailabilityConflictException("La hora de fin debe tener formato HH:mm.");
            }

            if (parsedEnd <= startTime)
            {
                throw new AvailabilityConflictException("La hora de fin debe ser posterior a la hora de inicio.");
            }

            endTime = parsedEnd;
        }

        if (request.DefaultCapacity <= 0)
        {
            throw new AvailabilityConflictException("El cupo por defecto debe ser mayor a 0.");
        }

        DateTime now = DateTime.UtcNow;
        ExperienceSchedule schedule = new()
        {
            Id = Guid.NewGuid(),
            ExperienceId = experienceId,
            DayOfWeek = request.DayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            DefaultCapacity = request.DefaultCapacity,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await availabilityRepository.AddScheduleAsync(schedule, cancellationToken);
        await availabilityRepository.SaveChangesAsync(cancellationToken);

        return MapSchedule(schedule);
    }

    public async Task DeleteScheduleAsync(
        Guid userId,
        bool isAdmin,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        ExperienceSchedule schedule = await availabilityRepository.GetScheduleByIdAsync(scheduleId, cancellationToken)
            ?? throw new ExperienceScheduleNotFoundException(scheduleId);

        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(schedule.ExperienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(schedule.ExperienceId);

        EnsureCanManage(experience, userId, isAdmin);

        await availabilityRepository.DeleteScheduleAsync(schedule, cancellationToken);
        await availabilityRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AvailabilityDto>> GenerateFromSchedulesAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        GenerateAvailabilitiesRequest request,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(experienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        EnsureCanManage(experience, userId, isAdmin);

        if (!DateOnly.TryParse(request.FromDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly fromDate)
            || !DateOnly.TryParse(request.ToDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly toDate))
        {
            throw new AvailabilityConflictException("Las fechas deben tener formato YYYY-MM-DD.");
        }

        if (toDate < fromDate || (toDate.DayNumber - fromDate.DayNumber) > 90)
        {
            throw new AvailabilityConflictException("El rango de fechas debe ser válido y no superar los 90 días.");
        }

        IReadOnlyList<ExperienceSchedule> schedules = await availabilityRepository.GetSchedulesByExperienceAsync(
            experienceId,
            cancellationToken);

        List<ExperienceSchedule> activeSchedules = schedules.Where(s => s.IsActive).ToList();
        if (activeSchedules.Count == 0)
        {
            throw new AvailabilityConflictException("La experiencia no tiene plantillas de horario activas.");
        }

        List<Availability> created = [];
        DateTime now = DateTime.UtcNow;

        for (DateOnly current = fromDate; current <= toDate; current = current.AddDays(1))
        {
            short dow = (short)current.DayOfWeek;
            foreach (ExperienceSchedule schedule in activeSchedules.Where(s => s.DayOfWeek == dow))
            {
                bool exists = await availabilityRepository.ExistsSlotAsync(
                    experienceId,
                    current,
                    schedule.StartTime,
                    null,
                    cancellationToken);

                if (exists)
                {
                    continue;
                }

                created.Add(new Availability
                {
                    Id = Guid.NewGuid(),
                    ExperienceId = experienceId,
                    ScheduleId = schedule.Id,
                    Date = current,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    TotalCapacity = request.OverrideCapacity ?? schedule.DefaultCapacity,
                    ReservedCapacity = 0,
                    SpecialPrice = request.SpecialPrice.HasValue ? decimal.Round(request.SpecialPrice.Value, 2) : null,
                    Status = AvailabilityStatus.ABIERTA,
                    Version = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Experience = experience
                });
            }
        }

        if (created.Count > 0)
        {
            await availabilityRepository.AddRangeAsync(created, cancellationToken);
            await availabilityRepository.SaveChangesAsync(cancellationToken);
        }

        return created.Select(a => MapToDto(a, experience)).ToList();
    }

    public static AvailabilityDto MapToDto(Availability a, Experience? experience)
    {
        decimal basePrice = experience?.Price ?? 0m;
        decimal effectivePrice = a.SpecialPrice ?? basePrice;
        int availableSpots = Math.Max(0, a.TotalCapacity - a.ReservedCapacity);

        return new AvailabilityDto(
            a.Id,
            a.ExperienceId,
            experience?.Title ?? string.Empty,
            experience?.Slug ?? string.Empty,
            a.ScheduleId,
            a.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            a.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            a.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            a.TotalCapacity,
            a.ReservedCapacity,
            availableSpots,
            basePrice,
            a.SpecialPrice,
            effectivePrice,
            experience?.Currency ?? "USD",
            a.Status.ToString(),
            a.CancellationReason,
            a.CreatedAt,
            a.UpdatedAt);
    }

    private static ExperienceScheduleDto MapSchedule(ExperienceSchedule s)
    {
        return new ExperienceScheduleDto(
            s.Id,
            s.ExperienceId,
            s.DayOfWeek,
            s.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            s.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            s.DefaultCapacity,
            s.IsActive,
            s.CreatedAt);
    }

    private static void EnsureCanManage(Experience experience, Guid userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        if (experience.Company is null || experience.Company.OperatorId != userId)
        {
            throw new ExperienceAccessDeniedException("No tienes permisos para administrar la disponibilidad de esta experiencia.");
        }
    }

    private static DateOnly? TryParseDate(string? raw)
    {
        return string.IsNullOrWhiteSpace(raw)
            ? null
            : DateOnly.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
                ? parsed
                : null;
    }
}
