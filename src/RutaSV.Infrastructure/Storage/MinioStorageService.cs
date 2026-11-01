using Microsoft.Extensions.Configuration;

using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;

using RutaSV.Application.Common.Storage;

namespace RutaSV.Infrastructure.Storage;

public sealed class MinioStorageService : IFileStorageService
{
    private const string DefaultBucketName = "rutasv-documents";
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;

    public MinioStorageService(IConfiguration configuration)
    {
        string endpoint = configuration["MinIO:Endpoint"] ?? "localhost:9000";
        string accessKey = configuration["MinIO:AccessKey"] ?? "rutasv_admin";
        string secretKey = configuration["MinIO:SecretKey"] ?? "rutasv_secret_2026";
        _bucketName = configuration["MinIO:BucketName"] ?? DefaultBucketName;

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    public async Task UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        bool bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName),
            cancellationToken);

        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucketName),
                cancellationToken);
        }

        PutObjectArgs putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        _ = await _minioClient.PutObjectAsync(putArgs, cancellationToken);
    }

    public async Task<StoredFileResult?> GetObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            bool bucketExists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName),
                cancellationToken);

            if (!bucketExists)
            {
                return null;
            }

            ObjectStat stat = await _minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectKey),
                cancellationToken);

            MemoryStream memoryStream = new();

            GetObjectArgs getArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            _ = await _minioClient.GetObjectAsync(getArgs, cancellationToken);
            memoryStream.Position = 0;

            string contentType = !string.IsNullOrWhiteSpace(stat.ContentType)
                ? stat.ContentType
                : GuessContentType(objectKey);

            return new StoredFileResult(memoryStream, contentType);
        }
        catch (MinioException)
        {
            return null;
        }
    }

    private static string GuessContentType(string objectKey)
    {
        string ext = Path.GetExtension(objectKey).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
