using IssueManagement.Infrastructure.IntegrationTests.Infrastructure;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.ValueObjects;

namespace IssueManagement.Infrastructure.IntegrationTests.Repositories;

public class IssueRepositoryTests : BaseIntegrationTest
{
    public IssueRepositoryTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task GetById_ReturnsSeededIssue()
    {
        // seeded ID from .files/IssueData.json
        var seededId = Guid.Parse("75AA05A1-7360-4156-9C43-04AD7C246A07");

        var issue = await _issueRepository.GetByIdAsync(seededId, CancellationToken.None);

        Assert.NotNull(issue);
        Assert.Equal(seededId, issue!.ID);
        Assert.Equal("testtetet", issue.Title.Value);
        Assert.Equal(IssueType.SafetyDeficiency, issue.Type);
        Assert.Equal(IssueStatus.InProgress, issue.Status);
    }

    [Fact]
    public async Task GetAsync_FilterByStatus_ReturnsResults()
    {
        var results = await _issueRepository.GetAsync(IssueStatus.InProgress, null, CancellationToken.None);

        Assert.NotNull(results);
        Assert.True(results.Count >= 1);
        Assert.Contains(results, r => r.Status == IssueStatus.InProgress);
    }

    [Fact]
    public async Task Add_Update_Delete_Lifecycle_Works()
    {
        // Arrange - create new domain issue
        var location = IssueLocation.CreateElementLocation(12345);
        var issue = Issue.Create("ITest Title", "ITest Description", IssueType.QualityDefect, location, "integration-test");

        // Act - add
        await _issueRepository.AddAsync(issue, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert - added
        var fetched = await _issueRepository.GetByIdAsync(issue.ID, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("ITest Title", fetched!.Title.Value);

        // Act - update
        var newTitle = IssueTitle.Create("Updated Title");
        var newDesc = IssueDescription.Create("Updated Desc");
        issue.UpdateDetails(newTitle, newDesc, IssueType.DesignChange, location);
        await _issueRepository.UpdateAsync(issue, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert - updated
        var updated = await _issueRepository.GetByIdAsync(issue.ID, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated!.Title.Value);
        Assert.Equal(IssueType.DesignChange, updated.Type);

        // Act - delete
        await _issueRepository.DeleteAsync(issue.ID, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert - deleted
        var deleted = await _issueRepository.GetByIdAsync(issue.ID, CancellationToken.None);
        Assert.Null(deleted);
    }
}
