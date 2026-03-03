using IssueManagement.Domain.Enums;

namespace IssueManagement.Application.DTOs;

public sealed record IssueDto(
    Guid Id,
    string Title,
    string Description,
    IssueType Type,
    IssueStatus Status,
    IssueLocationDto Location,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? UpdatedAt,
    List<IssuePhotoDto> Photos,
    List<IssueStatusHistoryDto> StatusHistory);

public sealed record IssueLocationDto(LocationType LocationType, int? DbId, WorldPositionDto? WorldPosition);

public sealed record WorldPositionDto(double X, double Y, double Z);

public sealed record IssuePhotoDto(
    Guid Id,
    string BlobKey,
    string FileName,
    string ContentType,
    CorrectionStage CorrectionStage,
    DateTime UploadedAt,
    string? PresignedUrl);

public sealed record IssueStatusHistoryDto(Guid Id, IssueStatus Status, DateTime CreatedOn, string? Comment);
