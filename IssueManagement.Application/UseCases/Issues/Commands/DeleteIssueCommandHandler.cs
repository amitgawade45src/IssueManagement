using IssueManagement.Application.Abstractions;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace IssueManagement.Application.UseCases.Issues.Commands;

internal sealed class DeleteIssueCommandHandler(IIssueRepository _repository, IUnitOfWork _unitOfWork, ILogger<DeleteIssueCommandHandler> _logger) : ICommandHandler<DeleteIssueCommand>
{
    public async Task<Result> Handle(DeleteIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.DeleteAsync(request.Id, cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken); 
            if (result == 0)
            {
                _logger.LogError("No changes were saved to the database after attempting to delete the issue with ID {IssueId}.", request.Id);
                return Result.Failure(new Error("500", "Failed to delete the issue."));
            }
            return Result.Success("Successfully deleted!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the issue with ID {IssueId}.", request.Id);
            return Result.Failure(new Error("500", "Internal Error while deleting"));
        }
    }
}
