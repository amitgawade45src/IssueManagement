using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.ValueObjects;

namespace IssueManagement.Domain.Models;

public class Issue : AggregateRoot
{
    // „Ÿ„Ÿ Creation constructor „Ÿ„Ÿ
    private Issue(IssueTitle title, IssueDescription description, IssueType type, IssueLocation location, string createdBy, string? updatedBy = null, DateTime? updatedOn = null, DateTime? deletedOn = null) : base(Guid.NewGuid(), createdBy)
    {
        Title = title;
        Description = description;
        Type = type;
        Status = IssueStatus.Open;
        Location = location;
        CreatedOn = DateTime.UtcNow; 
        UpdatedBy = updatedBy;
        UpdatedOn = updatedOn;
        DeletedOn = deletedOn;
    }

    // „Ÿ„Ÿ Reconstitution constructor „Ÿ„Ÿ
    private Issue(Guid id, IssueTitle title, IssueDescription description, IssueType type, IssueStatus status, IssueLocation location, DateTime createdOn, string createdBy, DateTime? updatedOn, string? updatedBy, DateTime? deletedOn) : base(id, createdOn, createdBy, updatedOn, updatedBy, deletedOn)
    {
        Title = title;
        Description = description;
        Type = type;
        Status = status;
        Location = location;
    }

    public IssueTitle Title { get; private set; }
    public IssueDescription Description { get; private set; }
    public IssueType Type { get; private set; }
    public IssueStatus Status { get; private set; }
    public IssueLocation Location { get; private set; }

    private readonly List<IssuePhoto> _photos = [];
    public IReadOnlyCollection<IssuePhoto> Photos => _photos.AsReadOnly();

    private readonly List<IssueStatusHistory> _statusHistory = [];
    public IReadOnlyCollection<IssueStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    // „Ÿ„Ÿ Factory: create new „Ÿ„Ÿ
    public static Issue Create(string title, string description, IssueType type, IssueLocation location, string createdBy)
    {
        var issueTitle = IssueTitle.Create(title);
        var issueDescription = IssueDescription.Create(description);
        var issue = new Issue(issueTitle, issueDescription, type, location, createdBy) { };
        issue._statusHistory.Add(IssueStatusHistory.Create(issue.ID, IssueStatus.Open, createdBy, "Issue created"));
        return issue;
    }

    // „Ÿ„Ÿ Factory: reconstitute from persistence (no reflection) „Ÿ„Ÿ
    public static Issue Reconstitute(
        Guid id, IssueTitle title, IssueDescription description, IssueType type, IssueStatus status,
        IssueLocation location, DateTime createdOn, string createdBy, DateTime? updatedOn, string? updatedBy,
        DateTime? deletedOn, List<IssuePhoto> photos, List<IssueStatusHistory> statusHistory)
    {
        var issue = new Issue(id, title, description, type, status, location, createdOn, createdBy, updatedOn, updatedBy, deletedOn);
        issue._photos.AddRange(photos);
        issue._statusHistory.AddRange(statusHistory);
        return issue;
    }

    public void UpdateDetails(IssueTitle title, IssueDescription description, IssueType type, IssueLocation location)
    {
        Title = title;
        Description = description;
        Type = type;
        Location = location;
        UpdatedOn = DateTime.UtcNow;
    }

    public void ChangeStatus(IssueStatus newStatus, string changedBy, string? comment = null)
    {
        if (Status == newStatus) return;

        Status = newStatus;
        UpdatedOn = DateTime.UtcNow;

        _statusHistory.Add(IssueStatusHistory.Create(ID, newStatus, changedBy, comment));
    }

    public void AddPhoto(IssuePhoto photo)
    {
        if (photo.CorrectionStage == CorrectionStage.AfterCorrection && Status != IssueStatus.Done)
        {
            throw new InvalidOperationException(
                $"After-correction photos can only be added when the issue status is Done. Current status: {Status}.");
        }

        _photos.Add(photo);
        UpdatedOn = DateTime.UtcNow;
    }

    public void RemovePhoto(Guid photoId)
    {
        var photo = _photos.Find(p => p.ID == photoId)
            ?? throw new InvalidOperationException($"Photo {photoId} not found on issue {ID}.");

        _photos.Remove(photo);
        UpdatedOn = DateTime.UtcNow;
    }
}
