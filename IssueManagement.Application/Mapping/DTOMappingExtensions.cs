using IssueManagement.Application.DTOs;
using IssueManagement.Domain.Models;

namespace IssueManagement.Application.Mapping;

public static class DTOMappingExtensions
{
    public static IssueDto ToDto(this Issue issue, Dictionary<string, string>? presignedUrls = null)
    {
        return new IssueDto(
            issue.ID,
            issue.Title.Value,
            issue.Description.Value,
            issue.Type,
            issue.Status,
            issue.Location.ToDto(),
            issue.CreatedOn,
            issue.CreatedBy,
            issue.UpdatedOn,
            issue.Photos.Select(p => p.ToDto(presignedUrls)).ToList(),
            issue.StatusHistory.Select(h => h.ToDto()).OrderByDescending(h => h.CreatedOn).ToList());
    }

    public static IssueLocationDto ToDto(this Domain.ValueObjects.IssueLocation location)
    {
        return new IssueLocationDto(
            location.LocationType,
            location.DbId,
            location.WorldPosition is not null ? new WorldPositionDto(location.WorldPosition.X, location.WorldPosition.Y, location.WorldPosition.Z) : null);
    }

    public static IssuePhotoDto ToDto(this IssuePhoto photo, Dictionary<string, string>? presignedUrls = null)
    {
        string? url = null;
        presignedUrls?.TryGetValue(photo.BlobKey, out url);
        return new IssuePhotoDto(
            photo.ID,
            photo.BlobKey,
            photo.FileName,
            photo.ContentType,
            photo.CorrectionStage,
            photo.CreatedOn,
            url);
    }

    public static IssueStatusHistoryDto ToDto(this IssueStatusHistory history)
    {
        return new IssueStatusHistoryDto(
            history.ID,
            history.Status,
            history.CreatedOn,
            history.Comment);
    }
}
