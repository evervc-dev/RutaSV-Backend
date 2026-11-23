using RutaSV.Domain.Common;

namespace RutaSV.Domain.Bookings.Exception;

public sealed class RefundNotFoundException : DomainException
{
    public RefundNotFoundException(Guid refundId)
        : base($"No se encontró el reembolso con identificador '{refundId}'.")
    {
    }

    public RefundNotFoundException(string code)
        : base($"No se encontró el reembolso '{code}'.")
    {
    }
}
