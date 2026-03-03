using IssueManagement.Application.UseCases.Issues.Commands;
using IssueManagement.Application.UseCases.Issues.Queries;
using IssueManagement.Application.UseCases.Photos.Commands;
using IssueManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IssueManagement.Controllers;

[ApiController]
[Authorize(Policy = "ViewerOrAbove")]
[Route("api/[controller]/[action]")]
public class IssuesApiController(ISender sender) : ControllerBase
{
    private string CurrentUser => User.Identity?.Name ?? "unknown";

    // Retrieves an APS viewer access token.. 
    [HttpPost]
    public async Task<IActionResult> GetViewerToken(CancellationToken ct)
    {
        var result = await sender.Send(new GetIssueTokenQuery(), ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(new { access_token = result.Value.Token, expires_in = result.Value.ExpiresIn });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllIssues([FromQuery] IssueStatus? status, [FromQuery] IssueType? type, CancellationToken ct)
    {
        var result = await sender.Send(new GetIssuesQuery(status, type), ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetIssueById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetIssueByIdQuery(id), ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> CreateIssue([FromBody] CreateIssueRequest request, CancellationToken ct)
    {
        var command = new CreateIssueCommand(
            request.Title,
            request.Description,
            request.Type,
            request.LocationType,
            request.DbId,
            request.WorldX,
            request.WorldY,
            request.WorldZ,
            CurrentUser);

        var issue = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetIssueById), new { id = issue.Value.Id }, issue);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> UpdateIssue(Guid id, [FromBody] UpdateIssueRequest request, CancellationToken ct)
    {
        var command = new UpdateIssueCommand(
            id,
            request.Title,
            request.Description,
            request.Type,
            request.LocationType,
            request.DbId,
            request.WorldX,
            request.WorldY,
            request.WorldZ);

        var result = await sender.Send(command, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> ChangeIssueStatus(Guid id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        var command = new ChangeIssueStatusCommand(id, request.NewStatus, CurrentUser, request.Comment);
        var result = await sender.Send(command, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteIssue(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteIssueCommand(id), ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result);
    }

    [HttpPost("{issueId:guid}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> UploadPhoto(Guid issueId, IFormFile file, [FromForm] CorrectionStage correctionStage, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        await using var stream = file.OpenReadStream();
        var command = new AddPhotoCommand(issueId, file.FileName, file.ContentType, stream, correctionStage, CurrentUser);
        var result = await sender.Send(command, ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("{issueId:guid}/{photoId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RemovePhoto(Guid issueId, Guid photoId, CancellationToken ct)
    {
        var result = await sender.Send(new RemovePhotoCommand(issueId, photoId), ct);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });
        return Ok(result);
    }
}

// „Ÿ„Ÿ Request DTOs „Ÿ„Ÿ 
public sealed record CreateIssueRequest(
    string Title,
    string Description,
    IssueType Type,
    LocationType LocationType,
    int? DbId,
    double? WorldX,
    double? WorldY,
    double? WorldZ);

public sealed record ChangeStatusRequest(
    IssueStatus NewStatus,
    string? ChangedBy,
    string? Comment);

public sealed record UpdateIssueRequest(
    string Title,
    string Description,
    IssueType Type,
    LocationType LocationType,
    int? DbId,
    double? WorldX,
    double? WorldY,
    double? WorldZ);
