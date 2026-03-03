using IssueManagement.Application.Abstractions;
using IssueManagement.Application.DTOs;
using IssueManagement.Application.Interfaces;
using IssueManagement.Application.Mapping;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace IssueManagement.Application.UseCases.Photos.Commands;

internal sealed class AddPhotoCommandHandler(IIssueRepository _repository, IBlobStorageService _blobStorage, IUnitOfWork _unitOfWork, ILogger<AddPhotoCommandHandler> _logger) : ICommandHandler<AddPhotoCommand, IssuePhotoDto>
{
    public async Task<Result<IssuePhotoDto>> Handle(AddPhotoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var issue = await _repository.GetByIdAsync(request.IssueId, cancellationToken);
            if (issue is null)
            {
                _logger.LogError("Issue with ID {IssueId} not found", request.IssueId);
                return Result.Failure<IssuePhotoDto>(new Error("404", "Issue not found!"));
            }

            // Validate: after-correction photos are only allowed when the issue is Done
            if (request.CorrectionStage == CorrectionStage.AfterCorrection && issue.Status != IssueStatus.Done)
            {
                _logger.LogError("Rejected after-correction photo for issue {IssueId} because status is {Status}", request.IssueId, issue.Status);
                return Result.Failure<IssuePhotoDto>(new Error("400", "After-correction photos can only be added when the issue status is Done."));
            }

            var objectName = $"{request.IssueId}/{Guid.NewGuid()}{Path.GetExtension(request.FileName)}";

            var blobKey = await _blobStorage.UploadAsync(objectName, request.FileStream, request.ContentType, cancellationToken);
            if (blobKey.IsFailure)
            {
                _logger.LogError("Failed to upload photo for issue {IssueId}: {Error}", request.IssueId, blobKey.Error);
                return Result.Failure<IssuePhotoDto>(new Error("500", "Failed to upload photo!"));
            }

            var photo = IssuePhoto.Create(request.IssueId, blobKey.Value, request.FileName, request.ContentType, request.CorrectionStage, request.createdBy);
            issue.AddPhoto(photo);
            await _repository.UpdateAsync(issue, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var presignedUrl = await _blobStorage.GetPresignedUrlAsync(blobKey.Value, cancellationToken);
            if (presignedUrl.IsFailure)
            {
                _logger.LogError("Failed to generate presigned URL for photo {BlobKey} of issue {IssueId}: {Error}", blobKey.Value, request.IssueId, presignedUrl.Error);
                return Result.Failure<IssuePhotoDto>(new Error("500", "Failed to generate presigned URL!"));
            }

            var urls = new Dictionary<string, string> { [blobKey.Value] = presignedUrl.Value };
            return Result.Success(photo.ToDto(urls));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding photo to issue {IssueId}", request.IssueId);
            return Result.Failure<IssuePhotoDto>(new Error("500", "Internal server error!"));
        }
    }
}
