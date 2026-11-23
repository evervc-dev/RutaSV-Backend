using RutaSV.Domain.Common;

namespace RutaSV.Domain.Identity.Exception;

public sealed class CompanyNotFoundException : DomainException
{
    public CompanyNotFoundException(Guid companyId)
        : base($"No se encontró la empresa con identificador '{companyId}'.")
    {
    }

    public CompanyNotFoundException(string identifier)
        : base($"No se encontró la empresa '{identifier}'.")
    {
    }
}
