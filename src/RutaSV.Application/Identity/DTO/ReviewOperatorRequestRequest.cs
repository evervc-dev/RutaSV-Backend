namespace RutaSV.Application.Identity.DTO;

public sealed record ReviewOperatorRequestRequest(
    bool IsApproved,
    string? RejectionReason = null
);
