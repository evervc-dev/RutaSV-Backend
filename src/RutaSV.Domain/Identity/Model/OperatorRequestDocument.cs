using RutaSV.Domain.Identity.Enum;

namespace RutaSV.Domain.Identity.Model;

public class OperatorRequestDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public OperatorRequestDocumentType Type { get; set; }
    public string? ObjectKey { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public OperatorRequest? OperatorRequest { get; set; }
}
