using IssueManagement.Application.Abstractions;
using IssueManagement.Application.UseCases.Issues.Commands;
using IssueManagement.ApplicationUnitTests.Stubs;
using IssueManagement.Domain.Repositories;
using Moq;

namespace IssueManagement.ApplicationUnitTests.Issues.Commands;

public class DeleteIssueCommandHandlerTests
{
    private readonly Mock<IIssueRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<TestLogger<DeleteIssueCommandHandler>> _logger;
    private readonly DeleteIssueCommandHandler _handler;

    public DeleteIssueCommandHandlerTests()
    {
        _repository = new Mock<IIssueRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<TestLogger<DeleteIssueCommandHandler>>();
        _handler = new DeleteIssueCommandHandler(_repository.Object, _unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ValidId_DeletesAndReturnsSuccess()
    {
        var id = Guid.NewGuid();
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteIssueCommand(id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SaveReturnsZero_ReturnsFailure500()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var command = new DeleteIssueCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }

    [Fact]
    public async Task Handle_RepositoryThrows_ReturnsFailure500()
    {
        _repository.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var command = new DeleteIssueCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }
}
