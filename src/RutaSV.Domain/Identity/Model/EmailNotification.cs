using RutaSV.Domain.Identity.Enum;

namespace RutaSV.Domain.Identity.Model;

public class EmailNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public Guid? ReservationId { get; set; }
    public NotificationType Type { get; set; } = NotificationType.VERIFICAR_EMAIL;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailDeliveryStatus Status { get; set; } = EmailDeliveryStatus.PENDIENTE;
    public short Attempts { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    public User? User { get; set; }
}
