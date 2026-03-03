using IssueManagement.Application.Abstractions;
using IssueManagement.Application.DTOs;
using IssueManagement.Application.Interfaces;
using IssueManagement.Application.Mapping;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace IssueManagement.Application.UseCases.Issues.Queries;

internal sealed class GetIssueByIdQueryHandler(IIssueRepository _repository, IBlobStorageService blobStorage, ILogger<GetIssueByIdQueryHandler> _logger) : IQueryHandler<GetIssueByIdQuery, IssueDto?>
{ 
    public async Task<Result<IssueDto?>> Handle(GetIssueByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var issue = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (issue is null)
            {
                _logger.LogError("Issue with ID {IssueId} not found.", request.Id);
                return Result.Failure<IssueDto?>(new Error("500", "Issue not found"));
            }
            var presignedUrls = new Dictionary<string, string>();
            foreach (var photo in issue.Photos)
            {
                var presignedUrl = await blobStorage.GetPresignedUrlAsync(photo.BlobKey, cancellationToken);
                if (presignedUrl.IsFailure)
                {
                    _logger.LogError("Failed to generate presigned URL for photo {BlobKey} of issue {IssueId}: {Error}", photo.BlobKey, request.Id, presignedUrl.Error);
                    continue; // Skip this photo and continue with others
                }
                presignedUrls[photo.BlobKey] = presignedUrl.Value;
            }
            return Result.Success<IssueDto?>(issue.ToDto(presignedUrls));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving issue with ID {IssueId}", request.Id);
            return Result.Failure<IssueDto?>(new Error("500", "An error occurred while retrieving the issue"));
        }
    }
}
