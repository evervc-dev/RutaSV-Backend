namespace RutaSV.Application.Identity.DTO;

public sealed record CreateOperatorRequestRequest(
    CreateOperatorCompanyRequest Company,
    string? Message = null,
    IReadOnlyList<CreateOperatorRequestDocumentRequest>? Documents = null
);
