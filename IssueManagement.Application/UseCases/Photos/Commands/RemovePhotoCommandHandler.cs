using IssueManagement.Application.Abstractions;
using IssueManagement.Application.Interfaces;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace IssueManagement.Application.UseCases.Photos.Commands;

internal sealed class RemovePhotoCommandHandler(IIssueRepository repository, IBlobStorageService blobStorage, IUnitOfWork _unitOfWork, ILogger<RemovePhotoCommandHandler> _logger) : ICommandHandler<RemovePhotoCommand>
{
    public async Task<Result> Handle(RemovePhotoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var _issue = await repository.GetByIdAsync(request.IssueId, cancellationToken);
            if (_issue == null)
            {
                _logger.LogError("Issue with ID {IssueId} not found.", request.IssueId);
                return Result.Failure(new Error("404", $"Issue with ID {request.IssueId} not found."));
            }

            var photo = _issue.Photos.FirstOrDefault(p => p.ID == request.PhotoId);
            if (photo == null)
            {
                _logger.LogError("photo with ID {PhotoId} not found.", request.PhotoId);
                return Result.Failure(new Error("404", $"photo with ID {request.PhotoId} not found."));
            }

            var deleteResult = await blobStorage.DeleteAsync(photo.BlobKey, cancellationToken);
            if (deleteResult.IsFailure)
            {
                _logger.LogError("Failed to delete photo {PhotoId} from blob storage. Error: {Error}", request.PhotoId, deleteResult.Error);
                return Result.Failure(new Error("500", "Failed to delete photo from storage."));
            }

            _issue.RemovePhoto(request.PhotoId);
            await repository.UpdateAsync(_issue, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while removing photo {PhotoId} from issue {IssueId}.", request.PhotoId, request.IssueId);
            return Result.Failure(new Error("500", "An error occurred while processing your request."));
        }
    }
}
