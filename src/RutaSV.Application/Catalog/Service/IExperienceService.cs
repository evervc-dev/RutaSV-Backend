using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Common.DTO;

namespace RutaSV.Application.Catalog.Service;

public interface IExperienceService
{
    Task<ExperienceDto> CreateAsync(
        Guid userId,
        bool isAdmin,
        CreateExperienceRequest request,
        CancellationToken cancellationToken = default);

    Task<ExperienceDto> UpdateAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        UpdateExperienceRequest request,
        CancellationToken cancellationToken = default);

    Task<ExperienceDto> PublishAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CancellationToken cancellationToken = default);

    Task<ExperienceDto> PauseAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CancellationToken cancellationToken = default);

    Task<ExperienceDto> WithdrawAsync(
        Guid adminUserId,
        Guid experienceId,
        WithdrawExperienceRequest request,
        CancellationToken cancellationToken = default);

    Task<ExperienceDto> RestoreAsync(
        Guid adminUserId,
        Guid experienceId,
        CancellationToken cancellationToken = default);

    Task<ExperienceDto> RemoveImageAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        Guid imageId,
        CancellationToken cancellationToken = default);

    Task<ExperienceDto> GetByIdOrSlugAsync(
        string slugOrId,
        Guid? currentUserId = null,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<ExperienceSummaryDto>> GetPublicPagedAsync(
        GetPublicExperiencesQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<ExperienceDto>> GetMyPagedAsync(
        Guid userId,
        bool isAdmin,
        GetMyExperiencesQuery query,
        CancellationToken cancellationToken = default);
}
