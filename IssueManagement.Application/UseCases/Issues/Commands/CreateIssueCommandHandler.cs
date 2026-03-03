using IssueManagement.Application.Abstractions;
using IssueManagement.Application.DTOs;
using IssueManagement.Application.Mapping;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using IssueManagement.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace IssueManagement.Application.UseCases.Issues.Commands;

internal sealed class CreateIssueCommandHandler(IIssueRepository _repository, IUnitOfWork _unitOfWork, ILogger<CreateIssueCommandHandler> _logger) : ICommandHandler<CreateIssueCommand, IssueDto>
{
    public async Task<Result<IssueDto>> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var issueLocation = BuildLocation(request);
            var issue = Issue.Create(request.Title, request.Description, request.Type, issueLocation, request.CreatedBy!);
            await _repository.AddAsync(issue, cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (result == 0)
            {
                _logger.LogError("Failed to save the {title} issue to the database.", request.Title);
                return Result.Failure<IssueDto>(new Error("500", "Failed to create the issue."));
            }
            return Result.Success(issue.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "message: {message}", ex.Message);
            return Result.Failure<IssueDto>(new Error("500", "An error occurred while creating the issue."));
        }
    }

    private static IssueLocation BuildLocation(CreateIssueCommand request)
    {
        var hasDbId = request.DbId.HasValue;
        var hasWorld = request.WorldX.HasValue && request.WorldY.HasValue && request.WorldZ.HasValue;

        return (request.LocationType, hasDbId, hasWorld) switch
        {
            (LocationType.ElementSpatial, true, true) =>
                IssueLocation.CreateCombinedLocation(request.DbId!.Value, new WorldPosition(request.WorldX!.Value, request.WorldY!.Value, request.WorldZ!.Value)),
            (LocationType.Element, true, false) =>
                IssueLocation.CreateElementLocation(request.DbId!.Value),
            (LocationType.Spatial, _, true) =>
                IssueLocation.CreateSpatialLocation(new WorldPosition(request.WorldX!.Value, request.WorldY!.Value, request.WorldZ!.Value)),
            _ => throw new ArgumentException("Invalid location data for the specified location type.")
        };
    }

}
