using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;

namespace IssueManagement.Domain.UnitTests.Models;

public class IssuePhotoTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsIssuePhoto()
    {
        var issueId = Guid.NewGuid();

        var photo = IssuePhoto.Create(issueId, "blob/key", "photo.jpg", "image/jpeg", CorrectionStage.BeforeCorrection, "user");

        Assert.NotEqual(Guid.Empty, photo.ID);
        Assert.Equal(issueId, photo.IssueId);
        Assert.Equal("blob/key", photo.BlobKey);
        Assert.Equal("photo.jpg", photo.FileName);
        Assert.Equal("image/jpeg", photo.ContentType);
        Assert.Equal(CorrectionStage.BeforeCorrection, photo.CorrectionStage);
        Assert.Equal("user", photo.CreatedBy);
    }
     
    [Fact]
    public void Reconstitute_RestoresAllFields()
    {
        var id = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var uploadedAt = DateTime.UtcNow.AddHours(-1);

        var photo = IssuePhoto.Reconstitute(id, issueId, "blob/key", "photo.png", "image/png", CorrectionStage.AfterCorrection, uploadedAt, "user");

        Assert.Equal(id, photo.ID);
        Assert.Equal(issueId, photo.IssueId);
        Assert.Equal("blob/key", photo.BlobKey);
        Assert.Equal("photo.png", photo.FileName);
        Assert.Equal("image/png", photo.ContentType);
        Assert.Equal(CorrectionStage.AfterCorrection, photo.CorrectionStage); 
    }
}
