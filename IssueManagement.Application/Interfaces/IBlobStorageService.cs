using IssueManagement.Domain.Abstractions;

namespace IssueManagement.Application.Interfaces;

public interface IBlobStorageService
{
    // Uploads a file to blob storage and returns the blob key (object path).
    Task<Result<string>> UploadAsync(string objectName, Stream data, string contentType, CancellationToken cancellationToken = default);
    // Generates a pre-signed URL for temporary read access.
    Task<Result<string>> GetPresignedUrlAsync(string objectName, CancellationToken cancellationToken = default);
    // Deletes an object from blob storage.
    Task<Result> DeleteAsync(string objectName, CancellationToken cancellationToken = default);
}
