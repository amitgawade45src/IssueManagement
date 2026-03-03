using IssueManagement.Application.Abstractions;
using IssueManagement.Application.Interfaces;
using IssueManagement.Application.UseCases.Photos.Commands;
using IssueManagement.ApplicationUnitTests.Helpers;
using IssueManagement.ApplicationUnitTests.Stubs;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using Moq;

namespace IssueManagement.ApplicationUnitTests.Photos.Commands;

public class RemovePhotoCommandHandlerTests
{
    private readonly Mock<IIssueRepository> _repository;
    private readonly Mock<IBlobStorageService> _blobStorage;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<TestLogger<RemovePhotoCommandHandler>> _logger;
    private readonly RemovePhotoCommandHandler _handler;

    public RemovePhotoCommandHandlerTests()
    {
        _repository = new Mock<IIssueRepository>();
        _blobStorage = new Mock<IBlobStorageService>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<TestLogger<RemovePhotoCommandHandler>>();
        _handler = new RemovePhotoCommandHandler(_repository.Object, _blobStorage.Object, _unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ValidPhoto_RemovesAndReturnsSuccess()
    {
        var issue = IssueFactory.CreateWithPhoto();
        var photo = issue.Photos.First();
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _blobStorage.Setup(b => b.DeleteAsync(photo.BlobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new RemovePhotoCommand(issue.ID, photo.ID);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Issue>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IssueNotFound_ReturnsFailure404()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Issue?)null);

        var command = new RemovePhotoCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("404", result.Error.Code);
    }

    [Fact]
    public async Task Handle_PhotoNotFound_ReturnsFailure404()
    {
        var issue = IssueFactory.CreateDefault(); // no photos
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);

        var command = new RemovePhotoCommand(issue.ID, Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("404", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BlobDeleteFails_ReturnsFailure500()
    {
        var issue = IssueFactory.CreateWithPhoto();
        var photo = issue.Photos.First();
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _blobStorage.Setup(b => b.DeleteAsync(photo.BlobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new Error("500", "Blob delete failed")));

        var command = new RemovePhotoCommand(issue.ID, photo.ID);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ReturnsFailure500()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected"));

        var command = new RemovePhotoCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }
}
