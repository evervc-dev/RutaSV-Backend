using RutaSV.Domain.Identity.Enum;

namespace RutaSV.Domain.Identity.Model;

public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperatorId { get; set; }
    public string TradeName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Description { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Whatsapp { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public int? MunicipalityId { get; set; }
    public string? Address { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.PENDIENTE;
    public string? RejectionReason { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public User? Operator { get; set; }
    public User? ApprovedByUser { get; set; }
    public ICollection<OperatorRequest> OperatorRequests { get; set; } = [];
}
