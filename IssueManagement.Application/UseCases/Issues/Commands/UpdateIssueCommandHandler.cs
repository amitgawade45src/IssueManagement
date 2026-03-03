using IssueManagement.Application.Abstractions;
using IssueManagement.Application.DTOs;
using IssueManagement.Application.Mapping;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Repositories;
using IssueManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IssueManagement.Application.UseCases.Issues.Commands;

internal sealed class UpdateIssueCommandHandler(IIssueRepository _repository, IUnitOfWork _unitOfWork, ILogger<UpdateIssueCommandHandler> _logger) : ICommandHandler<UpdateIssueCommand, IssueDto>
{
    public async Task<Result<IssueDto>> Handle(UpdateIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var issue = await _repository.GetByIdAsync(request.Id, cancellationToken); 
            if (issue is null)
            {
                _logger.LogError("Issue with ID {IssueId} not found.", request.Id);
                return Result.Failure<IssueDto>(new Error("404", "Issue not found.")); // fail fast logic
            }
            var title = IssueTitle.Create(request.Title);
            var description = IssueDescription.Create(request.Description);
            var location = BuildLocation(request);

            issue.UpdateDetails(title, description, request.Type, location);

            await _repository.UpdateAsync(issue, cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (result == 0)
            {
                _logger.LogError("Failed to update the issue with ID {IssueId} in the database.", request.Id);
                return Result.Failure<IssueDto>(new Error("500", "Failed to update the issue."));
            }
            return Result.Success(issue.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating issue {IssueId}", request.Id);
            return Result.Failure<IssueDto>(new Error("500", "An error occurred while updating the issue."));
        }
    }

    private static IssueLocation BuildLocation(UpdateIssueCommand request)
    {
        var hasDbId = request.DbId.HasValue;
        var hasWorld = request.WorldX.HasValue && request.WorldY.HasValue && request.WorldZ.HasValue;

        return (request.LocationType, hasDbId, hasWorld) switch
        {
            (LocationType.ElementSpatial, true, true) =>
                IssueLocation.CreateCombinedLocation(request.DbId!.Value,
                    new WorldPosition(request.WorldX!.Value, request.WorldY!.Value, request.WorldZ!.Value)),
            (LocationType.Element, true, true) =>
                IssueLocation.CreateCombinedLocation(request.DbId!.Value,
                    new WorldPosition(request.WorldX!.Value, request.WorldY!.Value, request.WorldZ!.Value)),
            (LocationType.Element, true, false) =>
                IssueLocation.CreateElementLocation(request.DbId!.Value),
            (LocationType.Spatial, _, true) =>
                IssueLocation.CreateSpatialLocation(
                    new WorldPosition(request.WorldX!.Value, request.WorldY!.Value, request.WorldZ!.Value)),
            _ => throw new ArgumentException("Invalid location data for the specified location type.")
        };
    }
}
