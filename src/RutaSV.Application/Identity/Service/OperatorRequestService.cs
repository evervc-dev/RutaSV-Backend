using FluentValidation;

using RutaSV.Application.Common.DTO;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Repository;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Exception;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Identity.Service;

public sealed class OperatorRequestService(
    IOperatorRequestRepository operatorRequestRepository,
    ICompanyRepository companyRepository,
    IUserRepository userRepository,
    IValidator<CreateOperatorRequestRequest> createValidator,
    IValidator<GetOperatorRequestsQuery> getPagedValidator,
    IValidator<ReviewOperatorRequestRequest> reviewValidator) : IOperatorRequestService
{
    public async Task<OperatorRequestDto> CreateAsync(
        Guid userId,
        CreateOperatorRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidCredentialsException("El usuario autenticado no existe o no se encuentra disponible.");

        bool hasPendingRequest = await operatorRequestRepository.HasPendingRequestByUserIdAsync(
            userId,
            cancellationToken);

        if (hasPendingRequest)
        {
            throw new PendingOperatorRequestAlreadyExistsException(userId);
        }

        DateTime now = DateTime.UtcNow;

        Company company = new()
        {
            Id = Guid.NewGuid(),
            OperatorId = user.Id,
            TradeName = request.Company.TradeName.Trim(),
            LegalName = request.Company.LegalName?.Trim(),
            Description = request.Company.Description?.Trim(),
            ContactEmail = request.Company.ContactEmail?.Trim().ToLowerInvariant(),
            ContactPhone = request.Company.ContactPhone?.Trim(),
            Whatsapp = request.Company.Whatsapp?.Trim(),
            Website = request.Company.Website?.Trim(),
            LogoUrl = request.Company.LogoUrl?.Trim(),
            MunicipalityId = request.Company.MunicipalityId,
            Address = request.Company.Address?.Trim(),
            Status = CompanyStatus.PENDIENTE,
            CreatedAt = now,
            UpdatedAt = now
        };

        OperatorRequest operatorRequest = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CompanyId = company.Id,
            Status = OperatorRequestStatus.PENDIENTE,
            Message = request.Message?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            User = user,
            Company = company
        };

        List<OperatorRequestDocument> documents = (request.Documents ?? [])
            .Select(doc => new OperatorRequestDocument
            {
                Id = Guid.NewGuid(),
                RequestId = operatorRequest.Id,
                Type = Enum.Parse<OperatorRequestDocumentType>(doc.Type.Trim(), ignoreCase: true),
                ObjectKey = doc.ObjectKey?.Trim(),
                Url = doc.Url?.Trim(),
                CreatedAt = now
            })
            .ToList();

        operatorRequest.Documents = documents;

        await operatorRequestRepository.ExecuteInTransactionAsync(async ct =>
        {
            await companyRepository.AddAsync(company, ct);
            await operatorRequestRepository.AddAsync(operatorRequest, ct);

            if (documents.Count > 0)
            {
                await operatorRequestRepository.AddDocumentsAsync(documents, ct);
            }
        }, cancellationToken);

        return MapToDto(operatorRequest, user, company, documents);
    }

    public async Task<OperatorRequestDto?> GetMyLatestAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        OperatorRequest? latest = await operatorRequestRepository.GetLatestByUserIdWithDetailsAsync(
            userId,
            cancellationToken);

        if (latest is null || latest.User is null || latest.Company is null)
        {
            return null;
        }

        return MapToDto(latest, latest.User, latest.Company, latest.Documents);
    }

    public async Task<OperatorRequestDto> UpdateMyRequestAsync(
        Guid userId,
        Guid requestId,
        CreateOperatorRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        OperatorRequest operatorRequest = await operatorRequestRepository.GetByIdWithDetailsAsync(
            requestId,
            cancellationToken)
            ?? throw new OperatorRequestNotFoundException(requestId);

        if (operatorRequest.UserId != userId)
        {
            throw new OperatorRequestNotFoundException(requestId);
        }

        if (operatorRequest.Status == OperatorRequestStatus.APROBADA)
        {
            throw new OperatorRequestAlreadyReviewedException(requestId, operatorRequest.Status.ToString());
        }

        User user = operatorRequest.User
            ?? await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new OperatorRequestNotFoundException(requestId);

        Company company = operatorRequest.Company
            ?? await companyRepository.GetByIdAsync(operatorRequest.CompanyId, cancellationToken)
            ?? throw new OperatorRequestNotFoundException(requestId);

        DateTime now = DateTime.UtcNow;

        company.TradeName = request.Company.TradeName.Trim();
        company.LegalName = request.Company.LegalName?.Trim();
        company.Description = request.Company.Description?.Trim();
        company.ContactEmail = request.Company.ContactEmail?.Trim().ToLowerInvariant();
        company.ContactPhone = request.Company.ContactPhone?.Trim();
        company.Whatsapp = request.Company.Whatsapp?.Trim();
        company.Website = request.Company.Website?.Trim();
        company.LogoUrl = request.Company.LogoUrl?.Trim();
        company.MunicipalityId = request.Company.MunicipalityId;
        company.Address = request.Company.Address?.Trim();
        company.Status = CompanyStatus.PENDIENTE;
        company.RejectionReason = null;
        company.ApprovedBy = null;
        company.ApprovedAt = null;
        company.UpdatedAt = now;

        operatorRequest.Message = request.Message?.Trim();
        operatorRequest.Status = OperatorRequestStatus.PENDIENTE;
        operatorRequest.RejectionReason = null;
        operatorRequest.ReviewedBy = null;
        operatorRequest.ReviewedAt = null;
        operatorRequest.UpdatedAt = now;

        List<OperatorRequestDocument> documents = (request.Documents ?? [])
            .Select(doc => new OperatorRequestDocument
            {
                Id = Guid.NewGuid(),
                RequestId = operatorRequest.Id,
                Type = Enum.Parse<OperatorRequestDocumentType>(doc.Type.Trim(), ignoreCase: true),
                ObjectKey = doc.ObjectKey?.Trim(),
                Url = doc.Url?.Trim(),
                CreatedAt = now
            })
            .ToList();

        await operatorRequestRepository.ExecuteInTransactionAsync(async ct =>
        {
            await companyRepository.UpdateAsync(company, ct);
            await operatorRequestRepository.UpdateAsync(operatorRequest, ct);
            await operatorRequestRepository.ReplaceDocumentsAsync(operatorRequest.Id, documents, ct);
        }, cancellationToken);

        return MapToDto(operatorRequest, user, company, documents);
    }

    public async Task<PagedResponse<OperatorRequestDto>> GetPagedAsync(
        GetOperatorRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        await getPagedValidator.ValidateAndThrowAsync(query, cancellationToken);

        OperatorRequestStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            statusFilter = Enum.Parse<OperatorRequestStatus>(query.Status.Trim(), ignoreCase: true);
        }

        (IReadOnlyList<OperatorRequest> items, int totalRecords) = await operatorRequestRepository.GetPagedAsync(
            query.PageNumber,
            query.PageSize,
            statusFilter,
            cancellationToken);

        List<OperatorRequestDto> dtos = items
            .Select(r => MapToDto(
                r,
                r.User!,
                r.Company!,
                r.Documents))
            .ToList();

        return PagedResponse<OperatorRequestDto>.Create(
            dtos,
            totalRecords,
            query.PageNumber,
            query.PageSize);
    }

    public async Task<OperatorRequestDto> ReviewAsync(
        Guid requestId,
        Guid adminUserId,
        ReviewOperatorRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        await reviewValidator.ValidateAndThrowAsync(request, cancellationToken);

        OperatorRequest operatorRequest = await operatorRequestRepository.GetByIdWithDetailsAsync(
            requestId,
            cancellationToken)
            ?? throw new OperatorRequestNotFoundException(requestId);

        if (operatorRequest.Status != OperatorRequestStatus.PENDIENTE)
        {
            throw new OperatorRequestAlreadyReviewedException(requestId, operatorRequest.Status.ToString());
        }

        User user = operatorRequest.User
            ?? await userRepository.GetByIdAsync(operatorRequest.UserId, cancellationToken)
            ?? throw new OperatorRequestNotFoundException(requestId);

        Company company = operatorRequest.Company
            ?? await companyRepository.GetByIdAsync(operatorRequest.CompanyId, cancellationToken)
            ?? throw new OperatorRequestNotFoundException(requestId);

        DateTime now = DateTime.UtcNow;

        await operatorRequestRepository.ExecuteInTransactionAsync(async ct =>
        {
            operatorRequest.ReviewedBy = adminUserId;
            operatorRequest.ReviewedAt = now;
            operatorRequest.UpdatedAt = now;

            company.ApprovedBy = adminUserId;
            company.ApprovedAt = now;
            company.UpdatedAt = now;

            if (request.IsApproved)
            {
                operatorRequest.Status = OperatorRequestStatus.APROBADA;
                operatorRequest.RejectionReason = null;

                company.Status = CompanyStatus.APROBADA;
                company.RejectionReason = null;

                user.Role = UserRole.OPERADOR;
                user.UpdatedAt = now;

                await userRepository.UpdateAsync(user, ct);
            }
            else
            {
                string reason = request.RejectionReason!.Trim();

                operatorRequest.Status = OperatorRequestStatus.RECHAZADA;
                operatorRequest.RejectionReason = reason;

                company.Status = CompanyStatus.RECHAZADA;
                company.RejectionReason = reason;
            }

            await companyRepository.UpdateAsync(company, ct);
            await operatorRequestRepository.UpdateAsync(operatorRequest, ct);
        }, cancellationToken);

        return MapToDto(operatorRequest, user, company, operatorRequest.Documents);
    }

    private static OperatorRequestDto MapToDto(
        OperatorRequest request,
        User user,
        Company company,
        IEnumerable<OperatorRequestDocument> documents)
    {
        OperatorRequestUserDto userDto = new(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.Role.ToString(),
            user.Status.ToString());

        CompanyDto companyDto = new(
            company.Id,
            company.OperatorId,
            company.TradeName,
            company.LegalName,
            company.Description,
            company.ContactEmail,
            company.ContactPhone,
            company.Whatsapp,
            company.Website,
            company.LogoUrl,
            company.MunicipalityId,
            company.Address,
            company.Status.ToString(),
            company.RejectionReason,
            company.ApprovedBy,
            company.ApprovedAt,
            company.CreatedAt,
            company.UpdatedAt);

        List<OperatorRequestDocumentDto> documentDtos = documents
            .Where(d => d.DeletedAt == null)
            .Select(d => new OperatorRequestDocumentDto(
                d.Id,
                d.Type.ToString(),
                d.ObjectKey,
                !string.IsNullOrWhiteSpace(d.Url)
                    ? d.Url
                    : !string.IsNullOrWhiteSpace(d.ObjectKey)
                        ? $"/api/operator-requests/documents/file?objectKey={Uri.EscapeDataString(d.ObjectKey)}"
                        : null,
                d.CreatedAt))
            .ToList();

        return new OperatorRequestDto(
            request.Id,
            request.UserId,
            request.CompanyId,
            request.Status.ToString(),
            request.Message,
            request.RejectionReason,
            request.ReviewedBy,
            request.ReviewedAt,
            request.CreatedAt,
            request.UpdatedAt,
            userDto,
            companyDto,
            documentDtos);
    }
}
