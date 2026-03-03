using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;

namespace IssueManagement.Domain.UnitTests.Models;

public class IssueStatusHistoryTests
{
    [Fact]
    public void Create_ReturnsHistoryEntry()
    {
        var issueId = Guid.NewGuid();

        var entry = IssueStatusHistory.Create(issueId, IssueStatus.InProgress, "admin", "Starting");

        Assert.NotEqual(Guid.Empty, entry.ID);
        Assert.Equal(issueId, entry.IssueId);
        Assert.Equal(IssueStatus.InProgress, entry.Status);
        Assert.Equal("Starting", entry.Comment);
    }

    [Fact]
    public void Reconstitute_RestoresAllFields()
    {
        var id = Guid.NewGuid();
        var issueId = Guid.NewGuid();
        var updatedOn = DateTime.UtcNow.AddMinutes(-30);

        var entry = IssueStatusHistory.Reconstitute(id, issueId, IssueStatus.Done, DateTime.UtcNow, "admin", "admin", "Completed", updatedOn);

        Assert.Equal(id, entry.ID);
        Assert.Equal(issueId, entry.IssueId);
        Assert.Equal(IssueStatus.Done, entry.Status);
        Assert.Equal("Completed", entry.Comment);
    }
}
