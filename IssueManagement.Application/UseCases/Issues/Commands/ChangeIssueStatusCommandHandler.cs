using IssueManagement.Application.Abstractions;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace IssueManagement.Application.UseCases.Issues.Commands;

internal sealed class ChangeIssueStatusCommandHandler(IIssueRepository _repository, IUnitOfWork _unitOfWork, ILogger<ChangeIssueStatusCommandHandler> _logger) : ICommandHandler<ChangeIssueStatusCommand>
{
    public async Task<Result> Handle(ChangeIssueStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var _issue = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (_issue is null)
            {
                _logger.LogError("Issue with id {id} not found", request.Id);
                return Result.Failure(new Error("404", "Issue not found"));
            }
            _issue.ChangeStatus(request.NewStatus, request.ChangedBy, request.Comment);

            await _repository.UpdateAsync(_issue, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message: {message}", ex.Message);
            return Result.Failure(new Error("500", "Internal Error"));
        }
    }
}
