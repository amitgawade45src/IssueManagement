using IssueManagement.Application.Interfaces;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace IssueManagement.Infrastructure.Storage;

internal sealed class MinioBlobStorageService(IMinioClient minioClient, IOptions<MinIOOptions> _options, ILogger<MinioBlobStorageService> _logger) : IBlobStorageService
{
    private readonly MinIOOptions minIoValue = _options.Value;

    public async Task<Result<string>> UploadAsync(string objectName, Stream data, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var ensureBucketResult = await EnsureBucketExistsAsync(cancellationToken);
            if (!ensureBucketResult.IsSuccess)
            {
                _logger.LogCritical(message: "Failed to ensure bucket exists in MinIO. Error: {ErrorCode} - {ErrorName}", ensureBucketResult.Error.Code, ensureBucketResult.Error.Name);
                return Result.Failure<string>(new Error("500", "Failed to ensure bucket exists"));
            }

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(minIoValue.BucketName)
                .WithObject(objectName)
                .WithStreamData(data)
                .WithObjectSize(data.Length)
                .WithContentType(contentType);

            _logger.LogInformation("Uploading file '{ObjectName}' to bucket '{BucketName}' in MinIO", objectName, minIoValue.BucketName);
            await minioClient.PutObjectAsync(putObjectArgs, cancellationToken); 
            _logger.LogInformation("successfully uploaded file '{ObjectName}' to bucket '{BucketName}' in MinIO", objectName, minIoValue.BucketName);
            return Result.Success(objectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, message: "Error uploading file to MinIO");
            return Result.Failure<string>(new Error("500", ex.Message));
        }
    }

    public async Task<Result<string>> GetPresignedUrlAsync(string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            var presignedGetArgs = new PresignedGetObjectArgs()
                .WithBucket(minIoValue.BucketName)
                .WithObject(objectName)
                .WithExpiry(minIoValue.ExpiryInSeconds);

            var presignedUrl = await minioClient.PresignedGetObjectAsync(presignedGetArgs);
            _logger.LogInformation("Generated presigned URL for object '{ObjectName}' in bucket '{BucketName}'", objectName, minIoValue.BucketName);
            return Result.Success(presignedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, message: "Error getting presigned URL from MinIO");
            return Result.Failure<string>(new Error("500", ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(minIoValue.BucketName)
                .WithObject(objectName);

            await minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);
            _logger.LogInformation("Deleted object '{ObjectName}' from bucket '{BucketName}' in MinIO", objectName, minIoValue.BucketName);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, message: "Error deleting file from MinIO");
            return Result.Failure(new Error("500", ex.Message));
        }
    }

    private async Task<Result> EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(minIoValue.BucketName);
            bool exists = await minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!exists)
            {
                _logger.LogInformation("Bucket '{BucketName}' does not exist. Creating it.", minIoValue.BucketName);
                var makeBucketArgs = new MakeBucketArgs().WithBucket(minIoValue.BucketName);
                _logger.LogInformation("Creating bucket '{BucketName}' in MinIO", minIoValue.BucketName);
                await minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
                _logger.LogInformation("Successfully created bucket '{BucketName}' in MinIO", minIoValue.BucketName);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, message: "Error ensuring bucket exists in MinIO");
            return Result.Failure(new Error("500", ex.Message));
        }
    }
}
