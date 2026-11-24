using System.Globalization;

using FluentValidation;

using RutaSV.Application.Bookings.DTO;
using RutaSV.Application.Bookings.Repository;
using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Repository;
using RutaSV.Application.Catalog.Service;
using RutaSV.Application.Common.DTO;
using RutaSV.Application.Identity.Repository;
using RutaSV.Application.Identity.Service;
using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Bookings.Exception;
using RutaSV.Domain.Bookings.Model;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Exception;
using RutaSV.Domain.Catalog.Model;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Bookings.Service;

public interface IReservationIncidentCoordinator
{
    Task HandleAvailabilityCancelledAsync(
        Availability availability,
        Experience experience,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task HandleExperienceRemovedAsync(
        Experience experience,
        IncidentCause cause,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken = default);
}

public interface IReservationService : IReservationIncidentCoordinator
{
    Task<ReservationDto> CreateWebReservationAsync(
        Guid visitorId,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> CreateInPersonReservationAsync(
        Guid operatorUserId,
        bool isAdmin,
        CreateInPersonReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<ReservationDto>> GetMyReservationsAsync(
        Guid visitorId,
        int pageNumber,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<ReservationDto>> GetOperatorReservationsAsync(
        Guid userId,
        bool isAdmin,
        int pageNumber,
        int pageSize,
        string? status = null,
        Guid? experienceId = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> GetByIdAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<CancelPreviewDto> GetCancelPreviewAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> ConfirmAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> RejectAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        RejectReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> CompleteAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> RescheduleByOperatorAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        RescheduleReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> CancelAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancelReservationRequest? request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailabilityDto>> GetIncidentRescheduleOptionsAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> ResolveIncidentAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        ResolveIncidentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefundDto>> GetOperatorRefundsAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<RefundDto> GetRefundByIdOrCodeAsync(
        Guid userId,
        bool isAdmin,
        string idOrCode,
        CancellationToken cancellationToken = default);

    Task<RefundDto> CompleteRefundAsync(
        Guid userId,
        bool isAdmin,
        Guid refundId,
        CompleteRefundRequest? request,
        CancellationToken cancellationToken = default);
}

public sealed class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.ExperienceId).NotEmpty().WithMessage("La experiencia es obligatoria.");
        RuleFor(x => x.AvailabilityId).NotEmpty().WithMessage("Debes seleccionar una fecha y horario disponible.");
        RuleFor(x => x.PeopleCount)
            .GreaterThan((short)0).WithMessage("La cantidad de personas debe ser al menos 1.");
        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("El nombre de contacto es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre de contacto no puede superar los 150 caracteres.");
        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("El correo de contacto no es válido.")
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.ContactPhone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));
        RuleFor(x => x.SpecialNotes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.SpecialNotes));
    }
}

public sealed class CreateInPersonReservationRequestValidator : AbstractValidator<CreateInPersonReservationRequest>
{
    public CreateInPersonReservationRequestValidator()
    {
        RuleFor(x => x.ExperienceId).NotEmpty().WithMessage("La experiencia es obligatoria.");
        RuleFor(x => x.AvailabilityId).NotEmpty().WithMessage("La sesión es obligatoria.");
        RuleFor(x => x.PeopleCount)
            .GreaterThan((short)0).WithMessage("La cantidad de personas debe ser al menos 1.");
        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("El nombre del visitante es obligatorio.")
            .MaximumLength(150);
        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("El teléfono del visitante es obligatorio para reservas presenciales.")
            .MaximumLength(20);
        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("El correo de contacto no es válido.")
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}

public sealed class RejectReservationRequestValidator : AbstractValidator<RejectReservationRequest>
{
    public RejectReservationRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Debes indicar el motivo del rechazo.")
            .MaximumLength(500).WithMessage("El motivo no puede superar los 500 caracteres.");
    }
}

public sealed class ReservationService(
    IReservationRepository reservationRepository,
    IExperienceRepository experienceRepository,
    IAvailabilityRepository availabilityRepository,
    IUserRepository userRepository,
    IEmailService emailService,
    IValidator<CreateReservationRequest> createWebValidator,
    IValidator<CreateInPersonReservationRequest> createInPersonValidator,
    IValidator<RejectReservationRequest> rejectValidator) : IReservationService
{
    public async Task<ReservationDto> CreateWebReservationAsync(
        Guid visitorId,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        await createWebValidator.ValidateAndThrowAsync(request, cancellationToken);

        User visitor = await userRepository.GetByIdAsync(visitorId, cancellationToken)
            ?? throw new ReservationAccessDeniedException("No se encontró la cuenta del usuario autenticado.");

        // RN-20: correo verificado obligatorio para reservar
        if (!visitor.EmailVerified || visitor.Status != UserStatus.ACTIVO)
        {
            throw new EmailNotVerifiedForBookingException();
        }

        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(request.ExperienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(request.ExperienceId);

        if (experience.DeletedAt.HasValue
            || experience.Status != ExperienceStatus.PUBLICADA
            || experience.Company?.Status != CompanyStatus.APROBADA)
        {
            throw new ReservationInvalidStateTransitionException("La experiencia seleccionada no está disponible para reservas.");
        }

        ValidatePeopleCount(experience, request.PeopleCount);

        Reservation createdReservation = null!;
        Availability lockedAvailability = null!;

        await reservationRepository.ExecuteInTransactionAsync(async ct =>
        {
            lockedAvailability = await availabilityRepository.GetByIdForUpdateAsync(request.AvailabilityId, ct)
                ?? throw new AvailabilityNotFoundException(request.AvailabilityId);

            if (lockedAvailability.ExperienceId != experience.Id)
            {
                throw new ReservationInvalidStateTransitionException("La sesión seleccionada no pertenece a esta experiencia.");
            }

            ValidateAvailabilityForBooking(lockedAvailability, request.PeopleCount);

            DateTime now = DateTime.UtcNow;
            decimal unitPrice = decimal.Round(lockedAvailability.SpecialPrice ?? experience.Price, 2);
            decimal total = decimal.Round(unitPrice * request.PeopleCount, 2);
            ReservationStatus initialStatus = experience.AutoConfirmation
                ? ReservationStatus.CONFIRMADA
                : ReservationStatus.PENDIENTE;

            string methodTag = string.IsNullOrWhiteSpace(request.PaymentMethod)
                ? "TARJETA_DEMO"
                : request.PaymentMethod.Trim().ToUpperInvariant();

            createdReservation = new Reservation
            {
                Id = Guid.NewGuid(),
                ReservationCode = BookingBusinessRules.GenerateReservationCode(now),
                ExperienceId = experience.Id,
                AvailabilityId = lockedAvailability.Id,
                VisitorId = visitor.Id,
                CreatedBy = visitor.Id,
                Source = ReservationSource.WEB,
                PeopleCount = request.PeopleCount,
                UnitPrice = unitPrice,
                Total = total,
                Currency = experience.Currency,
                Status = initialStatus,
                PaymentStatus = PaymentStatus.PAGADO_DEMO,
                PaymentReference = $"DEMO-{methodTag}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                ContactName = request.ContactName.Trim(),
                ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? visitor.Email : request.ContactEmail.Trim(),
                ContactPhone = request.ContactPhone?.Trim() ?? visitor.Phone,
                SpecialNotes = request.SpecialNotes?.Trim(),
                ConfirmedAt = initialStatus == ReservationStatus.CONFIRMADA ? now : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            lockedAvailability.ReservedCapacity += request.PeopleCount;
            if (lockedAvailability.ReservedCapacity >= lockedAvailability.TotalCapacity)
            {
                lockedAvailability.Status = AvailabilityStatus.AGOTADA;
            }

            lockedAvailability.Version++;
            lockedAvailability.UpdatedAt = now;

            ReservationHistory history = new()
            {
                Id = Guid.NewGuid(),
                ReservationId = createdReservation.Id,
                PreviousStatus = null,
                NewStatus = initialStatus,
                ChangedBy = visitor.Id,
                Notes = initialStatus == ReservationStatus.CONFIRMADA
                    ? "Reserva web creada y confirmada automáticamente (pago demo)."
                    : "Reserva web creada en estado pendiente de confirmación del operador (pago demo).",
                CreatedAt = now
            };

            await availabilityRepository.UpdateAsync(lockedAvailability, ct);
            await reservationRepository.AddAsync(createdReservation, ct);
            await reservationRepository.AddHistoryAsync(history, ct);
            await reservationRepository.SaveChangesAsync(ct);

            createdReservation.Experience = experience;
            createdReservation.Availability = lockedAvailability;
            createdReservation.Visitor = visitor;
            createdReservation.History.Add(history);
        }, cancellationToken);

        await SendReservationCreatedEmailsAsync(createdReservation, experience, lockedAvailability, cancellationToken);

        return MapToDto(createdReservation);
    }

    public async Task<ReservationDto> CreateInPersonReservationAsync(
        Guid operatorUserId,
        bool isAdmin,
        CreateInPersonReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        await createInPersonValidator.ValidateAndThrowAsync(request, cancellationToken);

        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(request.ExperienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(request.ExperienceId);

        EnsureIsOperatorOrAdmin(experience, operatorUserId, isAdmin);
        ValidatePeopleCount(experience, request.PeopleCount);

        Reservation createdReservation = null!;
        Availability lockedAvailability = null!;

        await reservationRepository.ExecuteInTransactionAsync(async ct =>
        {
            lockedAvailability = await availabilityRepository.GetByIdForUpdateAsync(request.AvailabilityId, ct)
                ?? throw new AvailabilityNotFoundException(request.AvailabilityId);

            if (lockedAvailability.ExperienceId != experience.Id)
            {
                throw new ReservationInvalidStateTransitionException("La sesión seleccionada no pertenece a esta experiencia.");
            }

            ValidateAvailabilityForBooking(lockedAvailability, request.PeopleCount);

            DateTime now = DateTime.UtcNow;
            decimal unitPrice = decimal.Round(lockedAvailability.SpecialPrice ?? experience.Price, 2);
            decimal total = decimal.Round(unitPrice * request.PeopleCount, 2);

            createdReservation = new Reservation
            {
                Id = Guid.NewGuid(),
                ReservationCode = BookingBusinessRules.GenerateReservationCode(now),
                ExperienceId = experience.Id,
                AvailabilityId = lockedAvailability.Id,
                VisitorId = null, // RN-19: reserva presencial sin cuenta visitante
                CreatedBy = operatorUserId,
                Source = ReservationSource.PRESENCIAL,
                PeopleCount = request.PeopleCount,
                UnitPrice = unitPrice,
                Total = total,
                Currency = experience.Currency,
                Status = ReservationStatus.CONFIRMADA,
                PaymentStatus = PaymentStatus.PAGADO_DEMO,
                PaymentReference = $"PRESENCIAL-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                ContactName = request.ContactName.Trim(),
                ContactEmail = request.ContactEmail?.Trim(),
                ContactPhone = request.ContactPhone.Trim(),
                SpecialNotes = request.SpecialNotes?.Trim(),
                ConfirmedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            lockedAvailability.ReservedCapacity += request.PeopleCount;
            if (lockedAvailability.ReservedCapacity >= lockedAvailability.TotalCapacity)
            {
                lockedAvailability.Status = AvailabilityStatus.AGOTADA;
            }

            lockedAvailability.Version++;
            lockedAvailability.UpdatedAt = now;

            ReservationHistory history = new()
            {
                Id = Guid.NewGuid(),
                ReservationId = createdReservation.Id,
                PreviousStatus = null,
                NewStatus = ReservationStatus.CONFIRMADA,
                ChangedBy = operatorUserId,
                Notes = "Reserva presencial registrada y confirmada por el operador.",
                CreatedAt = now
            };

            await availabilityRepository.UpdateAsync(lockedAvailability, ct);
            await reservationRepository.AddAsync(createdReservation, ct);
            await reservationRepository.AddHistoryAsync(history, ct);
            await reservationRepository.SaveChangesAsync(ct);

            createdReservation.Experience = experience;
            createdReservation.Availability = lockedAvailability;
            createdReservation.History.Add(history);
        }, cancellationToken);

        if (!string.IsNullOrWhiteSpace(createdReservation.ContactEmail))
        {
            string dateStr = lockedAvailability.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string timeStr = lockedAvailability.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture);
            (string subject, string html, string text) = BookingBusinessRules.BuildReservationCreatedTouristEmail(
                createdReservation, experience.Title, dateStr, timeStr);

            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                createdReservation.ContactEmail,
                createdReservation.ContactName,
                NotificationType.RESERVA_CONFIRMADA,
                subject,
                html,
                text,
                ReservationId: createdReservation.Id), cancellationToken);
        }

        return MapToDto(createdReservation);
    }

    public async Task<PagedResponse<ReservationDto>> GetMyReservationsAsync(
        Guid visitorId,
        int pageNumber,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        ReservationStatus? statusFilter = ParseStatus(status);

        (IReadOnlyList<Reservation> items, int total) = await reservationRepository.GetByVisitorPagedAsync(
            visitorId,
            pageNumber,
            pageSize,
            statusFilter,
            cancellationToken);

        return PagedResponse<ReservationDto>.Create(
            items.Select(MapToDto).ToList(),
            total,
            pageNumber,
            pageSize);
    }

    public async Task<PagedResponse<ReservationDto>> GetOperatorReservationsAsync(
        Guid userId,
        bool isAdmin,
        int pageNumber,
        int pageSize,
        string? status = null,
        Guid? experienceId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        ReservationStatus? statusFilter = ParseStatus(status);

        (IReadOnlyList<Reservation> items, int total) = await reservationRepository.GetByOperatorPagedAsync(
            isAdmin ? null : userId,
            pageNumber,
            pageSize,
            statusFilter,
            experienceId,
            search,
            cancellationToken);

        return PagedResponse<ReservationDto>.Create(
            items.Select(MapToDto).ToList(),
            total,
            pageNumber,
            pageSize);
    }

    public async Task<ReservationDto> GetByIdAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        Reservation reservation = await reservationRepository.GetByIdWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureCanAccessReservation(reservation, userId, isAdmin);
        return MapToDto(reservation);
    }

    public async Task<CancelPreviewDto> GetCancelPreviewAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        Reservation reservation = await reservationRepository.GetByIdWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureCanAccessReservation(reservation, userId, isAdmin);

        if (reservation.Status is not ReservationStatus.PENDIENTE and not ReservationStatus.CONFIRMADA)
        {
            return new CancelPreviewDto(
                reservation.Id,
                false,
                0,
                0,
                0,
                reservation.Currency,
                $"La reserva en estado {reservation.Status} no se puede cancelar.");
        }

        Availability? availability = reservation.Availability
            ?? await availabilityRepository.GetByIdAsync(reservation.AvailabilityId, cancellationToken);

        if (availability is null)
        {
            return new CancelPreviewDto(reservation.Id, false, 0, 0, 0, reservation.Currency, "Sesión no encontrada.");
        }

        double hours = BookingBusinessRules.GetHoursUntilStart(availability.Date, availability.StartTime);
        bool isOperatorSide = isAdmin || reservation.Experience?.Company?.OperatorId == userId;

        if (isOperatorSide && reservation.VisitorId != userId)
        {
            return new CancelPreviewDto(
                reservation.Id,
                true,
                Math.Round(hours, 2),
                100,
                decimal.Round(reservation.Total, 2),
                reservation.Currency,
                "Cancelación realizada por el operador: corresponde reembolso del 100 % al visitante.");
        }

        if (hours <= 0)
        {
            return new CancelPreviewDto(
                reservation.Id,
                false,
                Math.Round(hours, 2),
                0,
                0,
                reservation.Currency,
                "La experiencia ya inició o finalizó y no puede cancelarse.");
        }

        (short pct, decimal amount, _, string description) = BookingBusinessRules.CalculateVisitorCancellationRefund(
            reservation.Total,
            availability.Date,
            availability.StartTime);

        return new CancelPreviewDto(
            reservation.Id,
            true,
            Math.Round(hours, 2),
            pct,
            amount,
            reservation.Currency,
            description);
    }

    public async Task<ReservationDto> ConfirmAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        Reservation reservation = await reservationRepository.GetByIdWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureIsOperatorOrAdmin(reservation.Experience, userId, isAdmin);

        if (reservation.Status != ReservationStatus.PENDIENTE)
        {
            throw new ReservationInvalidStateTransitionException(
                $"Solo se pueden confirmar reservas en estado PENDIENTE (estado actual: {reservation.Status}).");
        }

        DateTime now = DateTime.UtcNow;
        ReservationStatus previous = reservation.Status;
        reservation.Status = ReservationStatus.CONFIRMADA;
        reservation.ConfirmedAt = now;
        reservation.UpdatedAt = now;

        ReservationHistory history = new()
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            PreviousStatus = previous,
            NewStatus = ReservationStatus.CONFIRMADA,
            ChangedBy = userId,
            Notes = "Reserva confirmada por el operador.",
            CreatedAt = now
        };

        reservation.History.Add(history);
        await reservationRepository.UpdateAsync(reservation, cancellationToken);
        await reservationRepository.AddHistoryAsync(history, cancellationToken);
        await reservationRepository.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(reservation.ContactEmail))
        {
            string expTitle = reservation.Experience?.Title ?? "Experiencia RutaSV";
            string dateStr = reservation.Availability?.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
            string timeStr = reservation.Availability?.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;

            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                reservation.ContactEmail,
                reservation.ContactName,
                NotificationType.RESERVA_CONFIRMADA,
                $"¡Tu reserva {reservation.ReservationCode} fue confirmada!",
                $"<p>¡Hola {reservation.ContactName}!</p><p>El operador confirmó tu reserva <strong>{reservation.ReservationCode}</strong> para <strong>{expTitle}</strong> el {dateStr} a las {timeStr}.</p>",
                $"Tu reserva {reservation.ReservationCode} para {expTitle} ({dateStr} {timeStr}) fue confirmada.",
                UserId: reservation.VisitorId,
                ReservationId: reservation.Id), cancellationToken);
        }

        return MapToDto(reservation);
    }

    public async Task<ReservationDto> RejectAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        RejectReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        await rejectValidator.ValidateAndThrowAsync(request, cancellationToken);

        Reservation reservation = await reservationRepository.GetByIdWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureIsOperatorOrAdmin(reservation.Experience, userId, isAdmin);

        if (reservation.Status != ReservationStatus.PENDIENTE)
        {
            throw new ReservationInvalidStateTransitionException(
                $"Solo se pueden rechazar reservas en estado PENDIENTE (estado actual: {reservation.Status}).");
        }

        Refund refund = null!;

        await reservationRepository.ExecuteInTransactionAsync(async ct =>
        {
            DateTime now = DateTime.UtcNow;
            ReservationStatus previous = reservation.Status;

            await ReleaseCapacityAsync(reservation.AvailabilityId, reservation.PeopleCount, now, ct);

            reservation.Status = ReservationStatus.RECHAZADA;
            reservation.CancellationReason = request.Reason.Trim();
            reservation.CancelledAt = now;
            reservation.CancelledBy = userId;
            reservation.PaymentStatus = PaymentStatus.REEMBOLSADO_DEMO;
            reservation.UpdatedAt = now;

            refund = new Refund
            {
                Id = Guid.NewGuid(),
                RefundCode = BookingBusinessRules.GenerateRefundCode(now),
                ReservationId = reservation.Id,
                Amount = decimal.Round(reservation.Total, 2),
                Percentage = 100,
                Reason = RefundReason.RESERVA_RECHAZADA,
                Status = RefundStatus.EN_PROCESO,
                Notes = request.Reason.Trim(),
                RequestedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
                Reservation = reservation
            };

            ReservationHistory history = new()
            {
                Id = Guid.NewGuid(),
                ReservationId = reservation.Id,
                PreviousStatus = previous,
                NewStatus = ReservationStatus.RECHAZADA,
                ChangedBy = userId,
                Notes = $"Reserva rechazada por el operador: {request.Reason.Trim()}. Ticket de reembolso 100 %: {refund.RefundCode}.",
                CreatedAt = now
            };

            reservation.Refund = refund;
            reservation.History.Add(history);

            await reservationRepository.UpdateAsync(reservation, ct);
            await reservationRepository.AddRefundAsync(refund, ct);
            await reservationRepository.AddHistoryAsync(history, ct);
            await reservationRepository.SaveChangesAsync(ct);
        }, cancellationToken);

        if (!string.IsNullOrWhiteSpace(reservation.ContactEmail))
        {
            string expTitle = reservation.Experience?.Title ?? "Experiencia RutaSV";
            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                reservation.ContactEmail,
                reservation.ContactName,
                NotificationType.RESERVA_RECHAZADA,
                $"Reserva {reservation.ReservationCode} rechazada — Reembolso 100 % iniciado",
                $"<p>Hola {reservation.ContactName},</p><p>Lamentablemente el operador rechazó tu reserva <strong>{reservation.ReservationCode}</strong> para <strong>{expTitle}</strong>.</p><p><strong>Motivo:</strong> {reservation.CancellationReason}</p><p>Se ha generado tu ticket de reembolso al 100 %: <strong>{refund.RefundCode}</strong> ({BookingBusinessRules.FormatMoney(refund.Amount, reservation.Currency)}).</p>",
                $"Tu reserva {reservation.ReservationCode} ({expTitle}) fue rechazada. Motivo: {reservation.CancellationReason}. Reembolso 100 %: {refund.RefundCode}.",
                UserId: reservation.VisitorId,
                ReservationId: reservation.Id), cancellationToken);
        }

        return MapToDto(reservation);
    }

    public async Task<ReservationDto> CompleteAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        Reservation reservation = await reservationRepository.GetByIdWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureIsOperatorOrAdmin(reservation.Experience, userId, isAdmin);

        if (reservation.Status != ReservationStatus.CONFIRMADA)
        {
            throw new ReservationInvalidStateTransitionException(
                $"Solo se pueden marcar como completadas reservas en estado CONFIRMADA (estado actual: {reservation.Status}).");
        }

        DateTime now = DateTime.UtcNow;
        ReservationStatus previous = reservation.Status;
        reservation.Status = ReservationStatus.COMPLETADA;
        reservation.CompletedAt = now;
        reservation.UpdatedAt = now;

        ReservationHistory history = new()
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            PreviousStatus = previous,
            NewStatus = ReservationStatus.COMPLETADA,
            ChangedBy = userId,
            Notes = "Reserva marcada como completada por el operador.",
            CreatedAt = now
        };

        reservation.History.Add(history);
        await reservationRepository.UpdateAsync(reservation, cancellationToken);
        await reservationRepository.AddHistoryAsync(history, cancellationToken);
        await reservationRepository.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(reservation.ContactEmail))
        {
            string expTitle = reservation.Experience?.Title ?? "Experiencia RutaSV";
            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                reservation.ContactEmail,
                reservation.ContactName,
                NotificationType.RESERVA_COMPLETADA,
                $"¡Gracias por vivir {expTitle} con RutaSV!",
                $"<div style=\"font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;\"><h2>¡Esperamos que hayas disfrutado la experiencia, {reservation.ContactName}!</h2><p>Tu reserva <strong>{reservation.ReservationCode}</strong> para <strong>{expTitle}</strong> fue marcada como <strong>COMPLETADA</strong>.</p></div>",
                $"Tu reserva {reservation.ReservationCode} ({expTitle}) fue completada. ¡Gracias por viajar con RutaSV!",
                UserId: reservation.VisitorId,
                ReservationId: reservation.Id), cancellationToken);
        }

        return MapToDto(reservation);
    }

    public async Task<ReservationDto> RescheduleByOperatorAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        RescheduleReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        Reservation reservation = await reservationRepository.GetByIdWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureIsOperatorOrAdmin(reservation.Experience, userId, isAdmin);

        if (reservation.Status is not ReservationStatus.PENDIENTE and not ReservationStatus.CONFIRMADA)
        {
            throw new ReservationInvalidStateTransitionException(
                $"No se puede modificar una reserva en estado {reservation.Status}.");
        }

        short newPeopleCount = request.PeopleCount ?? reservation.PeopleCount;
        if (reservation.Experience is not null)
        {
            ValidatePeopleCount(reservation.Experience, newPeopleCount);
        }

        Availability targetAvailability = null!;

        await reservationRepository.ExecuteInTransactionAsync(async ct =>
        {
            DateTime now = DateTime.UtcNow;
            bool sameSlot = request.AvailabilityId == reservation.AvailabilityId;

            if (sameSlot)
            {
                targetAvailability = await availabilityRepository.GetByIdForUpdateAsync(reservation.AvailabilityId, ct)
                    ?? throw new AvailabilityNotFoundException(reservation.AvailabilityId);

                int delta = newPeopleCount - reservation.PeopleCount;
                if (delta > 0 && targetAvailability.ReservedCapacity + delta > targetAvailability.TotalCapacity)
                {
                    throw new InsufficientCapacityException("No hay suficientes cupos disponibles en esta sesión.");
                }

                targetAvailability.ReservedCapacity = Math.Max(0, targetAvailability.ReservedCapacity + delta);
                UpdateAvailabilityOpenStatus(targetAvailability);
                targetAvailability.Version++;
                targetAvailability.UpdatedAt = now;
                await availabilityRepository.UpdateAsync(targetAvailability, ct);
            }
            else
            {
                await ReleaseCapacityAsync(reservation.AvailabilityId, reservation.PeopleCount, now, ct);

                targetAvailability = await availabilityRepository.GetByIdForUpdateAsync(request.AvailabilityId, ct)
                    ?? throw new AvailabilityNotFoundException(request.AvailabilityId);

                if (targetAvailability.ExperienceId != reservation.ExperienceId)
                {
                    throw new ReservationInvalidStateTransitionException("La nueva sesión debe pertenecer a la misma experiencia.");
                }

                ValidateAvailabilityForBooking(targetAvailability, newPeopleCount);

                targetAvailability.ReservedCapacity += newPeopleCount;
                UpdateAvailabilityOpenStatus(targetAvailability);
                targetAvailability.Version++;
                targetAvailability.UpdatedAt = now;
                await availabilityRepository.UpdateAsync(targetAvailability, ct);
            }

            reservation.AvailabilityId = targetAvailability.Id;
            reservation.PeopleCount = newPeopleCount;
            reservation.Total = decimal.Round(reservation.UnitPrice * newPeopleCount, 2);
            reservation.UpdatedAt = now;

            ReservationHistory history = new()
            {
                Id = Guid.NewGuid(),
                ReservationId = reservation.Id,
                PreviousStatus = reservation.Status,
                NewStatus = reservation.Status,
                ChangedBy = userId,
                Notes = string.IsNullOrWhiteSpace(request.Notes)
                    ? $"Reserva modificada por operador: nueva fecha {targetAvailability.Date:yyyy-MM-dd} {targetAvailability.StartTime:HH:mm}, {newPeopleCount} personas."
                    : request.Notes.Trim(),
                CreatedAt = now
            };

            await reservationRepository.UpdateAsync(reservation, ct);
            await reservationRepository.AddHistoryAsync(history, ct);
            await reservationRepository.SaveChangesAsync(ct);

            reservation.History.Add(history);
        }, cancellationToken);

        reservation.Availability = targetAvailability;

        if (!string.IsNullOrWhiteSpace(reservation.ContactEmail))
        {
            string expTitle = reservation.Experience?.Title ?? "Experiencia RutaSV";
            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                reservation.ContactEmail,
                reservation.ContactName,
                NotificationType.RESERVA_REAGENDADA,
                $"Tu reserva {reservation.ReservationCode} fue reprogramada",
                $"<p>Hola {reservation.ContactName},</p><p>El operador actualizó tu reserva <strong>{reservation.ReservationCode}</strong> para <strong>{expTitle}</strong>:</p><ul><li><strong>Nueva fecha:</strong> {targetAvailability.Date:yyyy-MM-dd} a las {targetAvailability.StartTime:HH:mm}</li><li><strong>Personas:</strong> {reservation.PeopleCount}</li><li><strong>Total:</strong> {BookingBusinessRules.FormatMoney(reservation.Total, reservation.Currency)}</li></ul>",
                $"Tu reserva {reservation.ReservationCode} ({expTitle}) fue reprogramada para el {targetAvailability.Date:yyyy-MM-dd} a las {targetAvailability.StartTime:HH:mm} ({reservation.PeopleCount} personas).",
                UserId: reservation.VisitorId,
                ReservationId: reservation.Id), cancellationToken);
        }

        return MapToDto(reservation);
    }

    public async Task<ReservationDto> CancelAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancelReservationRequest? request,
        CancellationToken cancellationToken = default)
    {
        Reservation reservation = await reservationRepository.GetByIdWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureCanAccessReservation(reservation, userId, isAdmin);

        if (reservation.Status is not ReservationStatus.PENDIENTE and not ReservationStatus.CONFIRMADA)
        {
            throw new ReservationInvalidStateTransitionException(
                $"No se puede cancelar una reserva en estado {reservation.Status}.");
        }

        bool isOperatorCancel = (isAdmin || reservation.Experience?.Company?.OperatorId == userId)
            && reservation.VisitorId != userId;

        Availability availability = reservation.Availability
            ?? await availabilityRepository.GetByIdAsync(reservation.AvailabilityId, cancellationToken)
            ?? throw new AvailabilityNotFoundException(reservation.AvailabilityId);

        short refundPercentage;
        decimal refundAmount;
        PaymentStatus resultingPaymentStatus;
        RefundReason refundReason;
        string policyNote;

        if (isOperatorCancel)
        {
            refundPercentage = 100;
            refundAmount = decimal.Round(reservation.Total, 2);
            resultingPaymentStatus = PaymentStatus.REEMBOLSADO_DEMO;
            refundReason = RefundReason.CANCELACION_OPERADOR;
            policyNote = "Cancelada por el operador (reembolso 100 %).";
        }
        else
        {
            double hours = BookingBusinessRules.GetHoursUntilStart(availability.Date, availability.StartTime);
            if (hours <= 0)
            {
                throw new ReservationInvalidStateTransitionException(
                    "No puedes cancelar una reserva después de la hora de inicio de la experiencia.");
            }

            (refundPercentage, refundAmount, resultingPaymentStatus, policyNote) =
                BookingBusinessRules.CalculateVisitorCancellationRefund(
                    reservation.Total,
                    availability.Date,
                    availability.StartTime);
            refundReason = RefundReason.CANCELACION_VISITANTE;
        }

        Refund? createdRefund = null;

        await reservationRepository.ExecuteInTransactionAsync(async ct =>
        {
            DateTime now = DateTime.UtcNow;
            ReservationStatus previous = reservation.Status;

            // RN-07: siempre liberar los cupos
            await ReleaseCapacityAsync(reservation.AvailabilityId, reservation.PeopleCount, now, ct);

            reservation.Status = ReservationStatus.CANCELADA;
            reservation.CancellationReason = request?.Reason?.Trim();
            reservation.CancelledAt = now;
            reservation.CancelledBy = userId;
            reservation.PaymentStatus = resultingPaymentStatus;
            reservation.UpdatedAt = now;

            if (refundPercentage > 0 && refundAmount > 0)
            {
                createdRefund = new Refund
                {
                    Id = Guid.NewGuid(),
                    RefundCode = BookingBusinessRules.GenerateRefundCode(now),
                    ReservationId = reservation.Id,
                    Amount = refundAmount,
                    Percentage = refundPercentage,
                    Reason = refundReason,
                    Status = RefundStatus.EN_PROCESO,
                    Notes = request?.Reason?.Trim() ?? policyNote,
                    RequestedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Reservation = reservation
                };

                reservation.Refund = createdRefund;
                await reservationRepository.AddRefundAsync(createdRefund, ct);
            }

            ReservationHistory history = new()
            {
                Id = Guid.NewGuid(),
                ReservationId = reservation.Id,
                PreviousStatus = previous,
                NewStatus = ReservationStatus.CANCELADA,
                ChangedBy = userId,
                Notes = createdRefund is not null
                    ? $"{policyNote} Ticket {createdRefund.RefundCode}."
                    : policyNote,
                CreatedAt = now
            };

            reservation.History.Add(history);

            await reservationRepository.UpdateAsync(reservation, ct);
            await reservationRepository.AddHistoryAsync(history, ct);
            await reservationRepository.SaveChangesAsync(ct);
        }, cancellationToken);

        await SendCancellationEmailsAsync(reservation, createdRefund, request?.Reason, cancellationToken);

        return MapToDto(reservation);
    }

    public async Task HandleAvailabilityCancelledAsync(
        Availability availability,
        Experience experience,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Reservation> affected = await reservationRepository.GetActiveFutureByAvailabilityAsync(
            availability.Id,
            cancellationToken);

        await MoveReservationsToIncidentDecisionAsync(
            affected,
            experience,
            IncidentCause.SESION_CANCELADA,
            NotificationType.SESION_CANCELADA_OPERADOR,
            actorUserId,
            reason,
            cancellationToken);
    }

    public async Task HandleExperienceRemovedAsync(
        Experience experience,
        IncidentCause cause,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        DateOnly todaySv = BookingBusinessRules.GetElSalvadorToday();
        IReadOnlyList<Reservation> affected = await reservationRepository.GetActiveFutureByExperienceAsync(
            experience.Id,
            todaySv,
            cancellationToken);

        NotificationType notifType = cause == IncidentCause.EXPERIENCIA_RETIRADA
            ? NotificationType.EXPERIENCIA_RETIRADA
            : NotificationType.EXPERIENCIA_ELIMINADA;

        await MoveReservationsToIncidentDecisionAsync(
            affected,
            experience,
            cause,
            notifType,
            actorUserId,
            reason,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AvailabilityDto>> GetIncidentRescheduleOptionsAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        Reservation reservation = await reservationRepository.GetByIdWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureCanAccessReservation(reservation, userId, isAdmin);

        ReservationIncident? incident = reservation.Incidents
            .FirstOrDefault(i => i.Status == IncidentStatus.PENDIENTE);

        if (reservation.Status != ReservationStatus.PENDIENTE_DECISION || incident is null)
        {
            throw new IncidentNotFoundException(reservationId);
        }

        Guid? operatorId = reservation.Experience?.Company?.OperatorId;
        if (!operatorId.HasValue)
        {
            return [];
        }

        DateOnly todaySv = BookingBusinessRules.GetElSalvadorToday();
        IReadOnlyList<Availability> operatorSlots = await availabilityRepository.GetByOperatorAsync(
            operatorId.Value,
            experienceId: null,
            fromDate: todaySv,
            toDate: null,
            cancellationToken);

        bool mustBeOtherExperience = incident.Cause is IncidentCause.EXPERIENCIA_ELIMINADA or IncidentCause.EXPERIENCIA_RETIRADA;

        List<AvailabilityDto> options = [];
        foreach (Availability slot in operatorSlots)
        {
            if (slot.Id == incident.OriginalAvailabilityId)
            {
                continue;
            }

            if (slot.Status != AvailabilityStatus.ABIERTA)
            {
                continue;
            }

            if (slot.TotalCapacity - slot.ReservedCapacity < reservation.PeopleCount)
            {
                continue;
            }

            if (BookingBusinessRules.GetHoursUntilStart(slot.Date, slot.StartTime) <= 0)
            {
                continue;
            }

            Experience? slotExp = slot.Experience;
            if (slotExp is null || slotExp.DeletedAt.HasValue || slotExp.Status != ExperienceStatus.PUBLICADA)
            {
                continue;
            }

            if (mustBeOtherExperience && slotExp.Id == reservation.ExperienceId)
            {
                continue;
            }

            // RN-21: debe tener el mismo precio unitario que la reserva original
            decimal slotPrice = decimal.Round(slot.SpecialPrice ?? slotExp.Price, 2);
            if (slotPrice != decimal.Round(reservation.UnitPrice, 2))
            {
                continue;
            }

            options.Add(AvailabilityService.MapToDto(slot, slotExp));
        }

        return options;
    }

    public async Task<ReservationDto> ResolveIncidentAsync(
        Guid userId,
        bool isAdmin,
        Guid reservationId,
        ResolveIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        Reservation reservation = await reservationRepository.GetByIdWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new ReservationNotFoundException(reservationId);

        EnsureCanAccessReservation(reservation, userId, isAdmin);

        if (reservation.Status != ReservationStatus.PENDIENTE_DECISION)
        {
            throw new ReservationInvalidStateTransitionException(
                "Esta reserva no tiene una incidencia pendiente de decisión.");
        }

        ReservationIncident incident = reservation.Incidents
            .FirstOrDefault(i => i.Status == IncidentStatus.PENDIENTE)
            ?? throw new IncidentNotFoundException(reservationId);

        string action = request.Action?.Trim().ToUpperInvariant() ?? string.Empty;

        if (action == "REEMBOLSAR")
        {
            Refund createdRefund = null!;
            await reservationRepository.ExecuteInTransactionAsync(async ct =>
            {
                DateTime now = DateTime.UtcNow;
                reservation.Status = ReservationStatus.CANCELADA;
                reservation.PaymentStatus = PaymentStatus.REEMBOLSADO_DEMO;
                reservation.CancelledAt = now;
                reservation.CancelledBy = userId;
                reservation.CancellationReason = string.IsNullOrWhiteSpace(request.Notes)
                    ? "Visitante solicitó reembolso del 100 % ante incidencia."
                    : request.Notes.Trim();
                reservation.UpdatedAt = now;

                RefundReason reason = incident.Cause == IncidentCause.SESION_CANCELADA
                    ? RefundReason.CANCELACION_OPERADOR
                    : RefundReason.EXPERIENCIA_ELIMINADA;

                createdRefund = new Refund
                {
                    Id = Guid.NewGuid(),
                    RefundCode = BookingBusinessRules.GenerateRefundCode(now),
                    ReservationId = reservation.Id,
                    Amount = decimal.Round(reservation.Total, 2),
                    Percentage = 100,
                    Reason = reason,
                    Status = RefundStatus.EN_PROCESO,
                    Notes = reservation.CancellationReason,
                    RequestedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Reservation = reservation
                };

                incident.Status = IncidentStatus.REEMBOLSADA;
                incident.ResolvedAt = now;
                incident.UpdatedAt = now;

                ReservationHistory history = new()
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    PreviousStatus = ReservationStatus.PENDIENTE_DECISION,
                    NewStatus = ReservationStatus.CANCELADA,
                    ChangedBy = userId,
                    Notes = $"Visitante eligió reembolso del 100 % ({createdRefund.RefundCode}).",
                    CreatedAt = now
                };

                reservation.Refund = createdRefund;
                reservation.History.Add(history);

                await reservationRepository.UpdateAsync(reservation, ct);
                await reservationRepository.UpdateIncidentAsync(incident, ct);
                await reservationRepository.AddRefundAsync(createdRefund, ct);
                await reservationRepository.AddHistoryAsync(history, ct);
                await reservationRepository.SaveChangesAsync(ct);
            }, cancellationToken);

            await SendCancellationEmailsAsync(reservation, createdRefund, reservation.CancellationReason, cancellationToken);
            return MapToDto(reservation);
        }

        if (action == "REAGENDAR")
        {
            if (!request.NewAvailabilityId.HasValue)
            {
                throw new ReservationInvalidStateTransitionException("Debes seleccionar una nueva sesión para reagendar.");
            }

            Availability newSlot = null!;
            Experience newExp = null!;

            await reservationRepository.ExecuteInTransactionAsync(async ct =>
            {
                newSlot = await availabilityRepository.GetByIdForUpdateAsync(request.NewAvailabilityId.Value, ct)
                    ?? throw new AvailabilityNotFoundException(request.NewAvailabilityId.Value);

                newExp = await experienceRepository.GetByIdWithDetailsAsync(newSlot.ExperienceId, ct)
                    ?? throw new ExperienceNotFoundException(newSlot.ExperienceId);

                if (newExp.DeletedAt.HasValue || newExp.Status != ExperienceStatus.PUBLICADA)
                {
                    throw new ReservationInvalidStateTransitionException("La experiencia destino no está publicada.");
                }

                Guid? originalOperatorId = reservation.Experience?.Company?.OperatorId;
                if (originalOperatorId.HasValue && newExp.Company?.OperatorId != originalOperatorId.Value)
                {
                    throw new ReservationInvalidStateTransitionException(
                        "Solo puedes reagendar en experiencias del mismo operador.");
                }

                if (incident.Cause is IncidentCause.EXPERIENCIA_ELIMINADA or IncidentCause.EXPERIENCIA_RETIRADA
                    && newExp.Id == reservation.ExperienceId)
                {
                    throw new ReservationInvalidStateTransitionException(
                        "La experiencia original fue retirada o archivada; debes seleccionar otra experiencia del mismo operador.");
                }

                decimal targetUnitPrice = decimal.Round(newSlot.SpecialPrice ?? newExp.Price, 2);
                if (targetUnitPrice != decimal.Round(reservation.UnitPrice, 2))
                {
                    throw new ReservationInvalidStateTransitionException(
                        $"La sesión elegida tiene un precio diferente ({targetUnitPrice:0.00}) al pagado originalmente ({reservation.UnitPrice:0.00}).");
                }

                ValidateAvailabilityForBooking(newSlot, reservation.PeopleCount);

                DateTime now = DateTime.UtcNow;
                newSlot.ReservedCapacity += reservation.PeopleCount;
                UpdateAvailabilityOpenStatus(newSlot);
                newSlot.Version++;
                newSlot.UpdatedAt = now;

                reservation.ExperienceId = newExp.Id;
                reservation.AvailabilityId = newSlot.Id;
                reservation.Status = ReservationStatus.CONFIRMADA;
                reservation.ConfirmedAt = now;
                reservation.UpdatedAt = now;

                incident.Status = IncidentStatus.REAGENDADA;
                incident.NewAvailabilityId = newSlot.Id;
                incident.ResolvedAt = now;
                incident.UpdatedAt = now;

                ReservationHistory history = new()
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    PreviousStatus = ReservationStatus.PENDIENTE_DECISION,
                    NewStatus = ReservationStatus.CONFIRMADA,
                    ChangedBy = userId,
                    Notes = $"Reserva reagendada a {newExp.Title} ({newSlot.Date:yyyy-MM-dd} {newSlot.StartTime:HH:mm}).",
                    CreatedAt = now
                };

                await availabilityRepository.UpdateAsync(newSlot, ct);
                await reservationRepository.UpdateAsync(reservation, ct);
                await reservationRepository.UpdateIncidentAsync(incident, ct);
                await reservationRepository.AddHistoryAsync(history, ct);
                await reservationRepository.SaveChangesAsync(ct);

                reservation.History.Add(history);
            }, cancellationToken);

            reservation.Experience = newExp;
            reservation.Availability = newSlot;

            if (!string.IsNullOrWhiteSpace(reservation.ContactEmail))
            {
                await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                    reservation.ContactEmail,
                    reservation.ContactName,
                    NotificationType.RESERVA_REAGENDADA,
                    $"Reserva {reservation.ReservationCode} reagendada con éxito",
                    $"<p>Hola {reservation.ContactName},</p><p>Tu reserva <strong>{reservation.ReservationCode}</strong> fue reagendada a <strong>{newExp.Title}</strong> el {newSlot.Date:yyyy-MM-dd} a las {newSlot.StartTime:HH:mm}.</p>",
                    $"Tu reserva {reservation.ReservationCode} fue reagendada a {newExp.Title} el {newSlot.Date:yyyy-MM-dd} {newSlot.StartTime:HH:mm}.",
                    UserId: reservation.VisitorId,
                    ReservationId: reservation.Id), cancellationToken);
            }

            User? opUser = newExp.Company?.Operator;
            string? opEmail = opUser?.Email ?? newExp.Company?.ContactEmail;
            if (!string.IsNullOrWhiteSpace(opEmail))
            {
                await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                    opEmail,
                    opUser?.FirstName ?? newExp.Company?.TradeName ?? "Operador",
                    NotificationType.RESERVA_REAGENDADA,
                    $"Reserva {reservation.ReservationCode} reagendada por el visitante",
                    $"<p>El visitante <strong>{reservation.ContactName}</strong> reagendó la reserva <strong>{reservation.ReservationCode}</strong> a <strong>{newExp.Title}</strong> el {newSlot.Date:yyyy-MM-dd} a las {newSlot.StartTime:HH:mm} ({reservation.PeopleCount} personas).</p>",
                    $"Reserva {reservation.ReservationCode} reagendada a {newExp.Title} ({newSlot.Date:yyyy-MM-dd} {newSlot.StartTime:HH:mm}).",
                    UserId: opUser?.Id,
                    ReservationId: reservation.Id,
                    RequireVerifiedEmail: true), cancellationToken);
            }

            return MapToDto(reservation);
        }

        throw new ReservationInvalidStateTransitionException(
            "La acción para resolver la incidencia debe ser 'REAGENDAR' o 'REEMBOLSAR'.");
    }

    public async Task<IReadOnlyList<RefundDto>> GetOperatorRefundsAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Refund> refunds = await reservationRepository.GetRefundsByOperatorAsync(
            isAdmin ? null : userId,
            cancellationToken);

        return refunds.Select(MapRefund).ToList();
    }

    public async Task<RefundDto> GetRefundByIdOrCodeAsync(
        Guid userId,
        bool isAdmin,
        string idOrCode,
        CancellationToken cancellationToken = default)
    {
        Refund refund = await reservationRepository.GetRefundByIdOrCodeAsync(idOrCode, cancellationToken)
            ?? throw new RefundNotFoundException(idOrCode);

        if (refund.Reservation is not null)
        {
            EnsureCanAccessReservation(refund.Reservation, userId, isAdmin);
        }

        return MapRefund(refund);
    }

    public async Task<RefundDto> CompleteRefundAsync(
        Guid userId,
        bool isAdmin,
        Guid refundId,
        CompleteRefundRequest? request,
        CancellationToken cancellationToken = default)
    {
        Refund refund = await reservationRepository.GetRefundByIdOrCodeAsync(refundId.ToString(), cancellationToken)
            ?? throw new RefundNotFoundException(refundId);

        if (refund.Reservation is not null)
        {
            EnsureIsOperatorOrAdmin(refund.Reservation.Experience, userId, isAdmin);
        }

        if (refund.Status == RefundStatus.REALIZADO)
        {
            return MapRefund(refund);
        }

        DateTime now = DateTime.UtcNow;
        refund.Status = RefundStatus.REALIZADO;
        refund.ProcessedAt = now;
        refund.ProcessedBy = userId;
        if (!string.IsNullOrWhiteSpace(request?.Notes))
        {
            refund.Notes = request.Notes.Trim();
        }

        refund.UpdatedAt = now;

        await reservationRepository.UpdateRefundAsync(refund, cancellationToken);
        await reservationRepository.SaveChangesAsync(cancellationToken);

        Reservation? res = refund.Reservation;
        if (res is not null && !string.IsNullOrWhiteSpace(res.ContactEmail))
        {
            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                res.ContactEmail,
                res.ContactName,
                NotificationType.REEMBOLSO_REALIZADO,
                $"Reembolso {refund.RefundCode} completado",
                $"<p>Hola {res.ContactName},</p><p>Tu reembolso <strong>{refund.RefundCode}</strong> por <strong>{BookingBusinessRules.FormatMoney(refund.Amount, res.Currency)}</strong> ({refund.Percentage} %) ha sido marcado como <strong>REALIZADO</strong>.</p>",
                $"Tu reembolso {refund.RefundCode} por {BookingBusinessRules.FormatMoney(refund.Amount, res.Currency)} ha sido completado.",
                UserId: res.VisitorId,
                ReservationId: res.Id), cancellationToken);
        }

        return MapRefund(refund);
    }

    private async Task MoveReservationsToIncidentDecisionAsync(
        IReadOnlyList<Reservation> affected,
        Experience experience,
        IncidentCause cause,
        NotificationType notificationType,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (affected.Count == 0)
        {
            return;
        }

        List<(Reservation Reservation, ReservationIncident Incident)> notificationsToSend = [];

        await reservationRepository.ExecuteInTransactionAsync(async ct =>
        {
            DateTime now = DateTime.UtcNow;
            DateTime deadline = now.AddMonths(1);

            foreach (Reservation res in affected)
            {
                if (res.Status is not ReservationStatus.PENDIENTE and not ReservationStatus.CONFIRMADA)
                {
                    continue;
                }

                ReservationStatus previous = res.Status;
                await ReleaseCapacityAsync(res.AvailabilityId, res.PeopleCount, now, ct);

                res.Status = ReservationStatus.PENDIENTE_DECISION;
                res.UpdatedAt = now;

                ReservationIncident incident = new()
                {
                    Id = Guid.NewGuid(),
                    ReservationId = res.Id,
                    Cause = cause,
                    Status = IncidentStatus.PENDIENTE,
                    Reason = reason,
                    OriginalAvailabilityId = res.AvailabilityId,
                    DecisionDeadline = deadline,
                    AutoResolved = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                ReservationHistory history = new()
                {
                    Id = Guid.NewGuid(),
                    ReservationId = res.Id,
                    PreviousStatus = previous,
                    NewStatus = ReservationStatus.PENDIENTE_DECISION,
                    ChangedBy = actorUserId,
                    Notes = $"Incidencia {cause}: el visitante tiene hasta {deadline:yyyy-MM-dd} para reagendar o solicitar reembolso del 100 %.",
                    CreatedAt = now
                };

                res.Incidents.Add(incident);
                res.History.Add(history);

                await reservationRepository.UpdateAsync(res, ct);
                await reservationRepository.AddIncidentAsync(incident, ct);
                await reservationRepository.AddHistoryAsync(history, ct);

                notificationsToSend.Add((res, incident));
            }

            await reservationRepository.SaveChangesAsync(ct);
        }, cancellationToken);

        foreach ((Reservation res, ReservationIncident incident) in notificationsToSend)
        {
            if (string.IsNullOrWhiteSpace(res.ContactEmail))
            {
                continue;
            }

            (string subject, string html, string text) = BookingBusinessRules.BuildIncidentNoticeEmail(
                res, experience.Title, incident);

            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                res.ContactEmail,
                res.ContactName,
                notificationType,
                subject,
                html,
                text,
                UserId: res.VisitorId,
                ReservationId: res.Id), cancellationToken);
        }
    }

    private async Task ReleaseCapacityAsync(Guid availabilityId, short peopleCount, DateTime now, CancellationToken ct)
    {
        Availability? slot = await availabilityRepository.GetByIdForUpdateAsync(availabilityId, ct);
        if (slot is null)
        {
            return;
        }

        slot.ReservedCapacity = Math.Max(0, slot.ReservedCapacity - peopleCount);
        if (slot.Status == AvailabilityStatus.AGOTADA && slot.ReservedCapacity < slot.TotalCapacity)
        {
            slot.Status = AvailabilityStatus.ABIERTA;
        }

        slot.Version++;
        slot.UpdatedAt = now;
        await availabilityRepository.UpdateAsync(slot, ct);
    }

    private async Task SendReservationCreatedEmailsAsync(
        Reservation reservation,
        Experience experience,
        Availability availability,
        CancellationToken cancellationToken)
    {
        string dateStr = availability.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string timeStr = availability.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(reservation.ContactEmail))
        {
            (string subject, string html, string text) = BookingBusinessRules.BuildReservationCreatedTouristEmail(
                reservation, experience.Title, dateStr, timeStr);

            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                reservation.ContactEmail,
                reservation.ContactName,
                reservation.Status == ReservationStatus.CONFIRMADA
                    ? NotificationType.RESERVA_CONFIRMADA
                    : NotificationType.RESERVA_CREADA,
                subject,
                html,
                text,
                UserId: reservation.VisitorId,
                ReservationId: reservation.Id), cancellationToken);
        }

        // RN-22: Notificar al operador solo si su correo está verificado (SmtpEmailService registra OMITIDO si no)
        User? operatorUser = experience.Company?.Operator;
        string? operatorEmail = operatorUser?.Email ?? experience.Company?.ContactEmail;
        if (!string.IsNullOrWhiteSpace(operatorEmail))
        {
            (string subject, string html, string text) = BookingBusinessRules.BuildNewReservationOperatorEmail(
                reservation, experience.Title, dateStr, timeStr);

            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                operatorEmail,
                operatorUser?.FirstName ?? experience.Company?.TradeName ?? "Operador",
                NotificationType.NUEVA_RESERVA_OPERADOR,
                subject,
                html,
                text,
                UserId: operatorUser?.Id,
                ReservationId: reservation.Id,
                RequireVerifiedEmail: true), cancellationToken);
        }
    }

    private async Task SendCancellationEmailsAsync(
        Reservation reservation,
        Refund? refund,
        string? reason,
        CancellationToken cancellationToken)
    {
        string expTitle = reservation.Experience?.Title ?? "Experiencia RutaSV";

        if (!string.IsNullOrWhiteSpace(reservation.ContactEmail))
        {
            (string subject, string html, string text) = BookingBusinessRules.BuildCancellationAndRefundEmail(
                reservation, expTitle, refund, reason);

            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                reservation.ContactEmail,
                reservation.ContactName,
                refund is not null ? NotificationType.REEMBOLSO_INICIADO : NotificationType.RESERVA_CANCELADA,
                subject,
                html,
                text,
                UserId: reservation.VisitorId,
                ReservationId: reservation.Id), cancellationToken);
        }

        User? operatorUser = reservation.Experience?.Company?.Operator;
        string? operatorEmail = operatorUser?.Email ?? reservation.Experience?.Company?.ContactEmail;
        if (!string.IsNullOrWhiteSpace(operatorEmail))
        {
            await emailService.SendNotificationEmailAsync(new EmailNotificationRequest(
                operatorEmail,
                operatorUser?.FirstName ?? reservation.Experience?.Company?.TradeName ?? "Operador",
                NotificationType.RESERVA_CANCELADA,
                $"Reserva {reservation.ReservationCode} cancelada — {expTitle}",
                $"<p>La reserva <strong>{reservation.ReservationCode}</strong> ({reservation.ContactName}, {reservation.PeopleCount} personas) fue cancelada y los cupos han sido liberados.</p>",
                $"La reserva {reservation.ReservationCode} fue cancelada y los cupos fueron liberados.",
                UserId: operatorUser?.Id,
                ReservationId: reservation.Id,
                RequireVerifiedEmail: true), cancellationToken);
        }
    }

    private static void ValidatePeopleCount(Experience experience, short peopleCount)
    {
        if (peopleCount < experience.MinPeople)
        {
            throw new InsufficientCapacityException(
                $"Esta experiencia requiere un mínimo de {experience.MinPeople} persona(s) por reserva.");
        }

        if (experience.HasLimitedCapacity && experience.MaxPeople.HasValue && peopleCount > experience.MaxPeople.Value)
        {
            throw new InsufficientCapacityException(
                $"Esta experiencia admite un máximo de {experience.MaxPeople.Value} personas por reserva.");
        }
    }

    private static void ValidateAvailabilityForBooking(Availability slot, short peopleCount)
    {
        if (slot.Status != AvailabilityStatus.ABIERTA)
        {
            throw new InsufficientCapacityException(
                $"La sesión seleccionada no está disponible para reservas (estado: {slot.Status}).");
        }

        if (BookingBusinessRules.GetHoursUntilStart(slot.Date, slot.StartTime) <= 0)
        {
            throw new InsufficientCapacityException("No se puede reservar una fecha u horario que ya pasó.");
        }

        int remaining = slot.TotalCapacity - slot.ReservedCapacity;
        if (remaining < peopleCount)
        {
            throw new InsufficientCapacityException(
                $"Solo quedan {remaining} cupo(s) disponibles para esta sesión.");
        }
    }

    private static void UpdateAvailabilityOpenStatus(Availability slot)
    {
        if (slot.Status is AvailabilityStatus.BLOQUEADA or AvailabilityStatus.CANCELADA)
        {
            return;
        }

        slot.Status = slot.ReservedCapacity >= slot.TotalCapacity
            ? AvailabilityStatus.AGOTADA
            : AvailabilityStatus.ABIERTA;
    }

    private static void EnsureCanAccessReservation(Reservation reservation, Guid userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        bool isVisitor = reservation.VisitorId.HasValue && reservation.VisitorId.Value == userId;
        bool isOperator = reservation.Experience?.Company?.OperatorId == userId;

        if (!isVisitor && !isOperator)
        {
            throw new ReservationAccessDeniedException("No tienes permisos para acceder a esta reserva.");
        }
    }

    private static void EnsureIsOperatorOrAdmin(Experience? experience, Guid userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        if (experience?.Company is null || experience.Company.OperatorId != userId)
        {
            throw new ReservationAccessDeniedException("No tienes permisos de operador sobre esta experiencia.");
        }
    }

    private static ReservationStatus? ParseStatus(string? raw)
    {
        return string.IsNullOrWhiteSpace(raw)
            ? null
            : Enum.TryParse<ReservationStatus>(raw.Trim(), ignoreCase: true, out ReservationStatus parsed)
                ? parsed
                : null;
    }

    public static RefundDto MapRefund(Refund refund)
    {
        Reservation? res = refund.Reservation;
        return new RefundDto(
            refund.Id,
            refund.RefundCode,
            refund.ReservationId,
            res?.ReservationCode ?? string.Empty,
            res?.Experience?.Title ?? string.Empty,
            res?.ContactName ?? string.Empty,
            res?.ContactEmail,
            refund.Amount,
            refund.Percentage,
            res?.Currency ?? "USD",
            refund.Reason.ToString(),
            refund.Status.ToString(),
            refund.Notes,
            refund.RequestedAt,
            refund.ProcessedAt,
            refund.ProcessedBy);
    }

    private static ReservationDto MapToDto(Reservation r)
    {
        Experience? exp = r.Experience;
        Availability? avail = r.Availability;

        string? coverUrl = exp?.Images
            .Where(i => i.RemovedAt == null)
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault()?.Url;

        string? locationLabel = exp?.Municipality is not null
            ? $"{exp.Municipality.Name}, {exp.Municipality.Department?.Name}".Trim(',', ' ')
            : exp?.ReferenceAddress;

        RefundDto? refundDto = r.Refund is not null ? MapRefund(r.Refund) : null;

        ReservationIncident? activeIncident = r.Incidents
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefault();

        ReservationIncidentDto? incidentDto = activeIncident is not null
            ? new ReservationIncidentDto(
                activeIncident.Id,
                activeIncident.Cause.ToString(),
                activeIncident.Status.ToString(),
                activeIncident.Reason,
                activeIncident.OriginalAvailabilityId,
                activeIncident.NewAvailabilityId,
                activeIncident.DecisionDeadline,
                activeIncident.ResolvedAt,
                activeIncident.AutoResolved,
                activeIncident.CreatedAt)
            : null;

        List<ReservationHistoryDto> historyDtos = r.History
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new ReservationHistoryDto(
                h.Id,
                h.PreviousStatus?.ToString(),
                h.NewStatus.ToString(),
                h.ChangedBy,
                h.Notes,
                h.CreatedAt))
            .ToList();

        return new ReservationDto(
            r.Id,
            r.ReservationCode,
            r.ExperienceId,
            exp?.Title ?? string.Empty,
            exp?.Slug ?? string.Empty,
            coverUrl,
            exp?.Company?.TradeName,
            locationLabel,
            r.AvailabilityId,
            avail?.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            avail?.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty,
            avail?.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            r.VisitorId,
            r.CreatedBy,
            r.Source.ToString(),
            r.PeopleCount,
            r.UnitPrice,
            r.Total,
            r.Currency,
            r.Status.ToString(),
            r.PaymentStatus.ToString(),
            r.PaymentReference,
            r.ContactName,
            r.ContactEmail,
            r.ContactPhone,
            r.SpecialNotes,
            r.CancellationReason,
            r.CancelledAt,
            r.ConfirmedAt,
            r.CompletedAt,
            r.CreatedAt,
            r.UpdatedAt,
            refundDto,
            incidentDto,
            historyDtos);
    }
}
