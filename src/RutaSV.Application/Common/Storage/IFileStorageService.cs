namespace RutaSV.Application.Common.Storage;

public sealed record StoredFileResult(Stream Stream, string ContentType);

public interface IFileStorageService
{
    Task UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<StoredFileResult?> GetObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}
