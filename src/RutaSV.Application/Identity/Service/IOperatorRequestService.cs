using RutaSV.Application.Common.DTO;
using RutaSV.Application.Identity.DTO;

namespace RutaSV.Application.Identity.Service;

public interface IOperatorRequestService
{
    Task<OperatorRequestDto> CreateAsync(
        Guid userId,
        CreateOperatorRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<OperatorRequestDto?> GetMyLatestAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<OperatorRequestDto> UpdateMyRequestAsync(
        Guid userId,
        Guid requestId,
        CreateOperatorRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<OperatorRequestDto>> GetPagedAsync(
        GetOperatorRequestsQuery query,
        CancellationToken cancellationToken = default);

    Task<OperatorRequestDto> ReviewAsync(
        Guid requestId,
        Guid adminUserId,
        ReviewOperatorRequestRequest request,
        CancellationToken cancellationToken = default);
}
