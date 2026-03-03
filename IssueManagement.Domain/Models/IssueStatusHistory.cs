using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Enums;

namespace IssueManagement.Domain.Models;

/// <summary>
/// An append-only transaction log entry recording a status transition.
/// The previous status is derived from the preceding row ordered by UpdatedOn.
/// </summary>
public sealed class IssueStatusHistory : Entity
{
    private IssueStatusHistory(Guid issueId, IssueStatus status, string createdBy, string? comment = null, string? updatedBy = null, DateTime? updatedOn = null) : base(Guid.NewGuid(), createdBy)
    {
        IssueId = issueId;
        Status = status;
        UpdatedBy = updatedBy;
        Comment = comment;
        UpdatedOn = updatedOn;
    }
    private IssueStatusHistory(Guid id, Guid issueId, IssueStatus status, DateTime createdOn, string createdBy, string? updatedBy, string? comment, DateTime? updatedOn) : base(id, createdOn, createdBy, updatedOn, updatedBy, null)
    {
        IssueId = issueId;
        Status = status;
        Comment = comment;
    }

    public Guid IssueId { get; private set; }
    public IssueStatus Status { get; private set; }     /// The status the issue transitioned *to* at this point in time.

    public string? Comment { get; private set; }

    public static IssueStatusHistory Create(Guid issueId, IssueStatus status, string createdBy, string? comment = null, string? updatedBy = null)
    {
        return new IssueStatusHistory(issueId, status, createdBy, comment);
    }

    public static IssueStatusHistory Reconstitute(Guid id, Guid issueId, IssueStatus status, DateTime createdOn, string creatdBy, string? updatedBy, string? comment, DateTime? updatedOn)
    {
        return new IssueStatusHistory(id, issueId, status, createdOn, creatdBy, updatedBy, comment, updatedOn);
    }
}
