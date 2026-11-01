namespace RutaSV.Application.Identity.DTO;

public sealed record OperatorRequestDocumentDto(
    Guid Id,
    string Type,
    string? ObjectKey,
    string? Url,
    DateTime CreatedAt
);
