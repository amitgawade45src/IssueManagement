using IssueManagement.Application.Abstractions;
using IssueManagement.Application.UseCases.Issues.Commands;
using IssueManagement.ApplicationUnitTests.Helpers;
using IssueManagement.ApplicationUnitTests.Stubs;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using Moq;

namespace IssueManagement.ApplicationUnitTests.Issues.Commands;

public class UpdateIssueCommandHandlerTests
{
    private readonly Mock<IIssueRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<TestLogger<UpdateIssueCommandHandler>> _logger;
    private readonly UpdateIssueCommandHandler _handler;

    public UpdateIssueCommandHandlerTests()
    {
        _repository = new Mock<IIssueRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<TestLogger<UpdateIssueCommandHandler>>();
        _handler = new UpdateIssueCommandHandler(_repository.Object, _unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ExistingIssue_UpdatesAndReturnsSuccess()
    {
        var issue = IssueFactory.CreateDefault();
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateIssueCommand(issue.ID, "Updated", "Updated Desc", IssueType.DesignChange, LocationType.Element, 50, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", result.Value.Title);
        Assert.Equal("Updated Desc", result.Value.Description);
        Assert.Equal(IssueType.DesignChange, result.Value.Type);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Issue>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IssueNotFound_ReturnsFailure404()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Issue?)null);

        var command = new UpdateIssueCommand(Guid.NewGuid(), "Title", "Desc", IssueType.QualityDefect, LocationType.Element, 1, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("404", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SaveReturnsZero_ReturnsFailure500()
    {
        var issue = IssueFactory.CreateDefault();
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var command = new UpdateIssueCommand(issue.ID, "T", "D", IssueType.QualityDefect, LocationType.Element, 1, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithSpatialLocation_UpdatesLocation()
    {
        var issue = IssueFactory.CreateDefault();
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateIssueCommand(issue.ID, "T", "D", IssueType.QualityDefect, LocationType.Spatial, null, 1.0, 2.0, 3.0);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LocationType.Spatial, result.Value.Location.LocationType);
    }
}
