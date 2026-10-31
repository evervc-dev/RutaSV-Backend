using RutaSV.Domain.Identity.Enum;

namespace RutaSV.Domain.Identity.Model;

public class OperatorRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public OperatorRequestStatus Status { get; set; } = OperatorRequestStatus.PENDIENTE;
    public string? Message { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Company? Company { get; set; }
    public User? ReviewedByUser { get; set; }
    public ICollection<OperatorRequestDocument> Documents { get; set; } = [];
}
