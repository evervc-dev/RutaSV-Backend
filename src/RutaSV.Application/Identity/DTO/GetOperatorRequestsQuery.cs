namespace RutaSV.Application.Identity.DTO;

public sealed record GetOperatorRequestsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Status = null
);
