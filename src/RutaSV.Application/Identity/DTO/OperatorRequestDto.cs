namespace RutaSV.Application.Identity.DTO;

public sealed record OperatorRequestDto(
    Guid Id,
    Guid UserId,
    Guid CompanyId,
    string Status,
    string? Message,
    string? RejectionReason,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    OperatorRequestUserDto User,
    CompanyDto Company,
    IReadOnlyList<OperatorRequestDocumentDto> Documents
);
