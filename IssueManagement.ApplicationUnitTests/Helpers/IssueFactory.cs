using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.ValueObjects;

namespace IssueManagement.ApplicationUnitTests.Helpers;

internal static class IssueFactory
{
    public static Issue CreateDefault(
        string title = "Test Issue",
        string description = "Test Description",
        IssueType type = IssueType.QualityDefect,
        IssueStatus status = IssueStatus.Open,
        string createdBy = "testuser")
    {
        var location = IssueLocation.CreateElementLocation(100);
        var issue = Issue.Create(title, description, type, location, createdBy);

        if (status != IssueStatus.Open)
        {
            issue.ChangeStatus(status, createdBy);
        }

        return issue;
    }

    public static Issue CreateWithPhoto(
        CorrectionStage correctionStage = CorrectionStage.BeforeCorrection,
        IssueStatus status = IssueStatus.Open)
    {
        var issue = CreateDefault(status: status);
        var photo = IssuePhoto.Create(issue.ID, $"blob/{Guid.NewGuid()}", "photo.jpg", "image/jpeg", correctionStage, "testuser");
        issue.AddPhoto(photo);
        return issue;
    }
}
