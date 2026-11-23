using System.Globalization;

using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Bookings.Model;

namespace RutaSV.Application.Bookings.Service;

public static class BookingBusinessRules
{
    /// <summary>
    /// El Salvador mantiene UTC-6 fijo durante todo el año (sin horario de verano).
    /// </summary>
    public static readonly TimeSpan ElSalvadorOffset = TimeSpan.FromHours(-6);

    public static DateTime GetElSalvadorNow()
    {
        return DateTime.UtcNow + ElSalvadorOffset;
    }

    public static DateOnly GetElSalvadorToday()
    {
        return DateOnly.FromDateTime(GetElSalvadorNow());
    }

    public static DateTime ToElSalvadorLocalStart(DateOnly date, TimeOnly startTime)
    {
        return date.ToDateTime(startTime);
    }

    public static double GetHoursUntilStart(DateOnly date, TimeOnly startTime, DateTime? nowUtc = null)
    {
        DateTime nowSv = (nowUtc ?? DateTime.UtcNow) + ElSalvadorOffset;
        DateTime sessionStartSv = ToElSalvadorLocalStart(date, startTime);
        return (sessionStartSv - nowSv).TotalHours;
    }

    public static (short Percentage, decimal RefundAmount, PaymentStatus ResultingPaymentStatus, string PolicyDescription)
        CalculateVisitorCancellationRefund(decimal total, DateOnly date, TimeOnly startTime, DateTime? nowUtc = null)
    {
        double hours = GetHoursUntilStart(date, startTime, nowUtc);

        if (hours >= 48d)
        {
            return (
                100,
                decimal.Round(total, 2),
                PaymentStatus.REEMBOLSADO_DEMO,
                "Cancelación con 48 horas o más de anticipación: corresponde reembolso del 100 %.");
        }

        if (hours >= 24d)
        {
            return (
                50,
                decimal.Round(total * 0.50m, 2),
                PaymentStatus.REEMBOLSADO_PARCIAL_DEMO,
                "Cancelación entre 24 y 48 horas antes del inicio: corresponde reembolso del 50 %.");
        }

        return (
            0,
            0m,
            PaymentStatus.PAGADO_DEMO,
            "Cancelación con menos de 24 horas de anticipación: no aplica reembolso (0 %), pero los cupos se liberan.");
    }

    public static string GenerateReservationCode(DateTime utcNow)
    {
        int year = (utcNow + ElSalvadorOffset).Year;
        string suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"RSV-{year}-{suffix}";
    }

    public static string GenerateRefundCode(DateTime utcNow)
    {
        int year = (utcNow + ElSalvadorOffset).Year;
        string suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"REF-{year}-{suffix}";
    }

    public static string FormatMoney(decimal amount, string currency = "USD")
    {
        return string.Create(CultureInfo.InvariantCulture, $"{currency} ${amount:0.00}");
    }

    public static (string Subject, string Html, string Text) BuildReservationCreatedTouristEmail(Reservation r, string experienceTitle, string dateStr, string timeStr)
    {
        bool confirmed = r.Status == ReservationStatus.CONFIRMADA;
        string statusText = confirmed ? "Confirmada automáticamente" : "Pendiente de confirmación del operador";
        string subject = confirmed
            ? $"Reserva confirmada {r.ReservationCode} — {experienceTitle}"
            : $"Reserva recibida {r.ReservationCode} — {experienceTitle}";

        string html = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2>¡Hola, {r.ContactName}!</h2>
                <p>Hemos registrado tu reserva en <strong>RutaSV</strong>.</p>
                <ul>
                    <li><strong>Código:</strong> {r.ReservationCode}</li>
                    <li><strong>Experiencia:</strong> {experienceTitle}</li>
                    <li><strong>Fecha y hora:</strong> {dateStr} a las {timeStr}</li>
                    <li><strong>Personas:</strong> {r.PeopleCount}</li>
                    <li><strong>Total pagado (demo):</strong> {FormatMoney(r.Total, r.Currency)}</li>
                    <li><strong>Estado:</strong> {statusText}</li>
                </ul>
                <p>Recuerda que puedes consultar el historial o gestionar tu reserva desde tu cuenta en RutaSV.</p>
            </div>
            """;

        string text = $"Reserva {r.ReservationCode} para {experienceTitle} el {dateStr} a las {timeStr}. Personas: {r.PeopleCount}. Total: {FormatMoney(r.Total, r.Currency)}. Estado: {statusText}.";
        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) BuildNewReservationOperatorEmail(Reservation r, string experienceTitle, string dateStr, string timeStr)
    {
        string subject = $"Nueva reserva {r.ReservationCode} en {experienceTitle}";
        string html = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2>Nueva reserva recibida</h2>
                <p>Se registró una nueva reserva en tu experiencia <strong>{experienceTitle}</strong>:</p>
                <ul>
                    <li><strong>Código:</strong> {r.ReservationCode}</li>
                    <li><strong>Visitante:</strong> {r.ContactName} ({r.ContactEmail ?? r.ContactPhone ?? "Sin contacto"})</li>
                    <li><strong>Fecha y hora:</strong> {dateStr} a las {timeStr}</li>
                    <li><strong>Personas:</strong> {r.PeopleCount}</li>
                    <li><strong>Total:</strong> {FormatMoney(r.Total, r.Currency)}</li>
                    <li><strong>Estado actual:</strong> {r.Status}</li>
                </ul>
            </div>
            """;
        string text = $"Nueva reserva {r.ReservationCode} de {r.ContactName} para {experienceTitle} ({dateStr} {timeStr}, {r.PeopleCount} personas). Estado: {r.Status}.";
        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) BuildCancellationAndRefundEmail(
        Reservation r,
        string experienceTitle,
        Refund? refund,
        string? reason)
    {
        string subject = $"Reserva {r.ReservationCode} cancelada — {experienceTitle}";
        string refundBlock = refund is not null
            ? $"<p><strong>Ticket de reembolso:</strong> {refund.RefundCode} ({refund.Percentage}% — {FormatMoney(refund.Amount, r.Currency)}, estado: {refund.Status}).</p>"
            : "<p>Según la política de cancelación (< 24 horas antes del inicio), esta cancelación no genera reembolso.</p>";

        string html = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2>Actualización de tu reserva {r.ReservationCode}</h2>
                <p>Tu reserva para <strong>{experienceTitle}</strong> ha sido cancelada.</p>
                {(string.IsNullOrWhiteSpace(reason) ? string.Empty : $"<p><strong>Motivo:</strong> {reason}</p>")}
                {refundBlock}
            </div>
            """;

        string refundText = refund is not null
            ? $"Reembolso {refund.RefundCode}: {refund.Percentage}% ({FormatMoney(refund.Amount, r.Currency)})."
            : "Sin reembolso aplicable (<24h).";
        string text = $"Reserva {r.ReservationCode} ({experienceTitle}) cancelada. {refundText}";
        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) BuildIncidentNoticeEmail(
        Reservation r,
        string experienceTitle,
        ReservationIncident incident)
    {
        string deadlineStr = (incident.DecisionDeadline + ElSalvadorOffset).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        string subject = $"Acción requerida en tu reserva {r.ReservationCode} — {experienceTitle}";
        string html = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2>Tu reserva {r.ReservationCode} requiere una decisión</h2>
                <p>La sesión programada para <strong>{experienceTitle}</strong> fue cancelada o retirada ({incident.Cause}).</p>
                {(string.IsNullOrWhiteSpace(incident.Reason) ? string.Empty : $"<p><strong>Motivo informado:</strong> {incident.Reason}</p>")}
                <p>Tienes hasta el <strong>{deadlineStr} (hora de El Salvador)</strong> para elegir en tu panel de Mis Reservas entre:</p>
                <ol>
                    <li><strong>Reagendar</strong> en otra fecha disponible del mismo precio y operador.</li>
                    <li>Solicitar <strong>reembolso del 100 %</strong> ({FormatMoney(r.Total, r.Currency)}).</li>
                </ol>
            </div>
            """;
        string text = $"Tu reserva {r.ReservationCode} ({experienceTitle}) pasó a PENDIENTE_DECISION. Puedes reagendar o pedir reembolso del 100% antes del {deadlineStr}.";
        return (subject, html, text);
    }
}
