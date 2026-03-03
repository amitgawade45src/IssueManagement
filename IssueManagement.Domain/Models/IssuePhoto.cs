using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Enums;

namespace IssueManagement.Domain.Models;

public sealed class IssuePhoto : Entity
{
    private IssuePhoto(Guid issueId, string blobKey, string fileName, string contentType, CorrectionStage correctionStage, string createdBy) : base(Guid.NewGuid(), createdBy)
    {
        IssueId = issueId;
        BlobKey = blobKey;
        FileName = fileName;
        ContentType = contentType;
        CorrectionStage = correctionStage;
    }

    private IssuePhoto(Guid id, Guid issueId, string blobKey, string fileName, string contentType, CorrectionStage correctionStage, DateTime createdOn, string createdBy) : base(id, createdOn, createdBy, null, null, null)
    {
        IssueId = issueId;
        BlobKey = blobKey;
        FileName = fileName;
        ContentType = contentType;
        CorrectionStage = correctionStage;
    }

    public Guid IssueId { get; private set; }
    public string BlobKey { get; private set; } = default!;
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public CorrectionStage CorrectionStage { get; private set; } 

    public static IssuePhoto Create(Guid issueId, string blobKey, string fileName, string contentType, CorrectionStage correctionStage, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        return new IssuePhoto(issueId, blobKey, fileName, contentType, correctionStage, createdBy);
    }

    public static IssuePhoto Reconstitute(Guid id, Guid issueId, string blobKey, string fileName, string contentType, CorrectionStage correctionStage, DateTime createdOn, string createdBy)
    {
        return new IssuePhoto(id, issueId, blobKey, fileName, contentType, correctionStage, createdOn, createdBy);
    }
}
