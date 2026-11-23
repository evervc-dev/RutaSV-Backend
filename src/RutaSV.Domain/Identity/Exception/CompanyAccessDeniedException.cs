using RutaSV.Domain.Common;

namespace RutaSV.Domain.Identity.Exception;

public sealed class CompanyAccessDeniedException(string message) : DomainException(message);
