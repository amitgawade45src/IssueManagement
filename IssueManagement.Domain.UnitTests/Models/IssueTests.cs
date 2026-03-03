using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.ValueObjects;

namespace IssueManagement.Domain.UnitTests.Models;

public class IssueTests
{
    private static IssueLocation DefaultLocation =>
        IssueLocation.CreateElementLocation(100);

    #region Create

    [Fact]
    public void Create_ValidInputs_ReturnsIssueWithOpenStatus()
    {
        var issue = Issue.Create("Title", "Description", IssueType.QualityDefect, DefaultLocation, "user@test.com");

        Assert.NotEqual(Guid.Empty, issue.ID);
        Assert.Equal("Title", issue.Title.Value);
        Assert.Equal("Description", issue.Description.Value);
        Assert.Equal(IssueType.QualityDefect, issue.Type);
        Assert.Equal(IssueStatus.Open, issue.Status);
        Assert.Equal("user@test.com", issue.CreatedBy);
        Assert.Empty(issue.Photos);
    }

    [Fact]
    public void Create_AddsInitialStatusHistory()
    {
        var issue = Issue.Create("Title", "Desc", IssueType.SafetyDeficiency, DefaultLocation, "user");

        Assert.Single(issue.StatusHistory);
        var entry = issue.StatusHistory.First();
        Assert.Equal(IssueStatus.Open, entry.Status);
    } 

    [Fact]
    public void UpdateDetails_UpdatesTitleDescriptionTypeAndLocation()
    {
        var issue = Issue.Create("Old", "OldDesc", IssueType.QualityDefect, DefaultLocation, "user");
        var newLocation = IssueLocation.CreateSpatialLocation(new WorldPosition(1, 2, 3));
        var newTitle = IssueTitle.Create("New");
        var newDescription = IssueDescription.Create("NewDesc");

        issue.UpdateDetails(newTitle, newDescription, IssueType.DesignChange, newLocation);

        Assert.Equal("New", issue.Title.Value);
        Assert.Equal("NewDesc", issue.Description.Value);
        Assert.Equal(IssueType.DesignChange, issue.Type);
        Assert.Equal(LocationType.Spatial, issue.Location.LocationType);
        Assert.NotNull(issue.UpdatedOn);
    }

    #endregion

    #region ChangeStatus

    [Fact]
    public void ChangeStatus_DifferentStatus_UpdatesStatusAndAddsHistory()
    {
        var issue = Issue.Create("T", "D", IssueType.QualityDefect, DefaultLocation, "user");

        issue.ChangeStatus(IssueStatus.InProgress, "admin", "Starting work");

        Assert.Equal(IssueStatus.InProgress, issue.Status);
        Assert.Equal(2, issue.StatusHistory.Count); // Open + InProgress
        Assert.NotNull(issue.UpdatedOn);
    }

    [Fact]
    public void ChangeStatus_SameStatus_NoChange()
    {
        var issue = Issue.Create("T", "D", IssueType.QualityDefect, DefaultLocation, "user");

        issue.ChangeStatus(IssueStatus.Open, "admin"); // same as initial

        Assert.Equal(IssueStatus.Open, issue.Status);
        Assert.Single(issue.StatusHistory); // only the initial one
    }

    [Fact]
    public void ChangeStatus_MultipleTimes_TracksAllHistory()
    {
        var issue = Issue.Create("T", "D", IssueType.QualityDefect, DefaultLocation, "user");

        issue.ChangeStatus(IssueStatus.InProgress, "admin");
        issue.ChangeStatus(IssueStatus.Done, "admin");

        Assert.Equal(IssueStatus.Done, issue.Status);
        Assert.Equal(3, issue.StatusHistory.Count); // Open + InProgress + Done
    }

    #endregion

    #region AddPhoto

    [Fact]
    public void AddPhoto_BeforeCorrection_AddsPhoto()
    {
        var issue = Issue.Create("T", "D", IssueType.QualityDefect, DefaultLocation, "user");
        var photo = IssuePhoto.Create(issue.ID, "blob/key", "photo.jpg", "image/jpeg", CorrectionStage.BeforeCorrection, "user");

        issue.AddPhoto(photo);

        Assert.Single(issue.Photos);
        Assert.NotNull(issue.UpdatedOn);
    }

    [Fact]
    public void AddPhoto_AfterCorrection_WhenStatusNotDone_ThrowsInvalidOperationException()
    {
        var issue = Issue.Create("T", "D", IssueType.QualityDefect, DefaultLocation, "user");
        var photo = IssuePhoto.Create(issue.ID, "blob/key", "photo.jpg", "image/jpeg", CorrectionStage.AfterCorrection, "user");

        Assert.Throws<InvalidOperationException>(() => issue.AddPhoto(photo));
    }

    [Fact]
    public void AddPhoto_AfterCorrection_WhenStatusIsDone_Succeeds()
    {
        var issue = Issue.Create("T", "D", IssueType.QualityDefect, DefaultLocation, "user");
        issue.ChangeStatus(IssueStatus.Done, "admin");
        var photo = IssuePhoto.Create(issue.ID, "blob/key", "photo.jpg", "image/jpeg", CorrectionStage.AfterCorrection, "user");

        issue.AddPhoto(photo);

        Assert.Single(issue.Photos);
    }

    #endregion

    #region RemovePhoto

    [Fact]
    public void RemovePhoto_ExistingPhoto_RemovesIt()
    {
        var issue = Issue.Create("T", "D", IssueType.QualityDefect, DefaultLocation, "user");
        var photo = IssuePhoto.Create(issue.ID, "blob/key", "photo.jpg", "image/jpeg", CorrectionStage.BeforeCorrection, "user");
        issue.AddPhoto(photo);

        issue.RemovePhoto(photo.ID);

        Assert.Empty(issue.Photos);
        Assert.NotNull(issue.UpdatedOn);
    }

    [Fact]
    public void RemovePhoto_NonExistingPhotoId_ThrowsInvalidOperationException()
    {
        var issue = Issue.Create("T", "D", IssueType.QualityDefect, DefaultLocation, "user");

        Assert.Throws<InvalidOperationException>(() => issue.RemovePhoto(Guid.NewGuid()));
    }

    #endregion

    #region Reconstitute

    [Fact]
    public void Reconstitute_RestoresAllFields()
    {
        var id = Guid.NewGuid();
        var title = IssueTitle.Create("Restored");
        var desc = IssueDescription.Create("Restored desc");
        var location = IssueLocation.CreateElementLocation(1);
        var createdOn = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var photos = new List<IssuePhoto>();
        var history = new List<IssueStatusHistory>();

        var issue = Issue.Reconstitute(id, title, desc, IssueType.ConstructionDefect, IssueStatus.InProgress,
            location, createdOn, "creator", null, null, null, photos, history);

        Assert.Equal(id, issue.ID);
        Assert.Equal("Restored", issue.Title.Value);
        Assert.Equal(IssueType.ConstructionDefect, issue.Type);
        Assert.Equal(IssueStatus.InProgress, issue.Status);
        Assert.Equal(createdOn, issue.CreatedOn);
        Assert.Equal("creator", issue.CreatedBy);
    }

    #endregion
}
