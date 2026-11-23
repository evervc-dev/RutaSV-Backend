using RutaSV.Domain.Common;

namespace RutaSV.Domain.Catalog.Exception;

public sealed class InvalidCatalogReferenceException(string message) : DomainException(message);
