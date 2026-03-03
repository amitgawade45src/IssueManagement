using IssueManagement.Application.Abstractions;
using IssueManagement.Application.UseCases.Issues.Commands;
using IssueManagement.ApplicationUnitTests.Helpers;
using IssueManagement.ApplicationUnitTests.Stubs;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using Moq;

namespace IssueManagement.ApplicationUnitTests.Issues.Commands;

public class ChangeIssueStatusCommandHandlerTests
{
    private readonly Mock<IIssueRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<TestLogger<ChangeIssueStatusCommandHandler>> _logger;
    private readonly ChangeIssueStatusCommandHandler _handler;

    public ChangeIssueStatusCommandHandlerTests()
    {
        _repository = new Mock<IIssueRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<TestLogger<ChangeIssueStatusCommandHandler>>();
        _handler = new ChangeIssueStatusCommandHandler(_repository.Object, _unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ExistingIssue_ChangesStatusAndReturnsSuccess()
    {
        var issue = IssueFactory.CreateDefault();
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new ChangeIssueStatusCommand(issue.ID, IssueStatus.InProgress, "admin", "Starting work");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Issue>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IssueNotFound_ReturnsFailure404()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Issue?)null);

        var command = new ChangeIssueStatusCommand(Guid.NewGuid(), IssueStatus.Done, "admin", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("404", result.Error.Code);
    }

    [Fact]
    public async Task Handle_RepositoryThrows_ReturnsFailure500()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var command = new ChangeIssueStatusCommand(Guid.NewGuid(), IssueStatus.Done, "admin", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SameStatus_StillReturnsSuccess()
    {
        var issue = IssueFactory.CreateDefault(); // status = Open
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new ChangeIssueStatusCommand(issue.ID, IssueStatus.Open, "admin", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
