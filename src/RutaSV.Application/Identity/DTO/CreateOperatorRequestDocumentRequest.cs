namespace RutaSV.Application.Identity.DTO;

public sealed record CreateOperatorRequestDocumentRequest(
    string Type,
    string? ObjectKey = null,
    string? Url = null
);
