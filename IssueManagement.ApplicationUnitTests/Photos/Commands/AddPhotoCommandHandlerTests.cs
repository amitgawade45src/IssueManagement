using IssueManagement.Application.Abstractions;
using IssueManagement.Application.Interfaces;
using IssueManagement.Application.UseCases.Photos.Commands;
using IssueManagement.ApplicationUnitTests.Helpers;
using IssueManagement.ApplicationUnitTests.Stubs;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace IssueManagement.ApplicationUnitTests.Photos.Commands;

public class AddPhotoCommandHandlerTests
{
    private readonly Mock<IIssueRepository> _repository;
    private readonly Mock<IBlobStorageService> _blobStorage;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<TestLogger<AddPhotoCommandHandler>> _logger;
    private readonly AddPhotoCommandHandler _handler;

    public AddPhotoCommandHandlerTests()
    {
        _repository = new Mock<IIssueRepository>();
        _blobStorage = new Mock<IBlobStorageService>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<TestLogger<AddPhotoCommandHandler>>();
        _handler = new AddPhotoCommandHandler(_repository.Object, _blobStorage.Object, _unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UploadsAndReturnsSuccess()
    {
        var issue = IssueFactory.CreateDefault();
        _repository.Setup(a => a.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);

        _blobStorage.Setup(a => a.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<string>("blob/key"));
        _blobStorage.Setup(a => a.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<string>("https://presigned.url"));
        _unitOfWork.Setup(a => a.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        using var stream = new MemoryStream();
        var command = new AddPhotoCommand(issue.ID, "photo.jpg", "image/jpeg", stream, CorrectionStage.BeforeCorrection, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("photo.jpg", result.Value.FileName);
        Assert.Equal("https://presigned.url", result.Value.PresignedUrl);
    }

    [Fact]
    public async Task Handle_IssueNotFound_ReturnsFailure404()
    {
        _repository.Setup(a => a.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Issue?)null);

        using var stream = new MemoryStream();
        var command = new AddPhotoCommand(Guid.NewGuid(), "photo.jpg", "image/jpeg", stream, CorrectionStage.BeforeCorrection, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("404", result.Error.Code);
    }

    [Fact]
    public async Task Handle_AfterCorrectionWhenNotDone_ReturnsFailure400()
    {
        var issue = IssueFactory.CreateDefault(); // status = Open
        _repository.Setup(a => a.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);

        using var stream = new MemoryStream();
        var command = new AddPhotoCommand(issue.ID, "photo.jpg", "image/jpeg", stream, CorrectionStage.AfterCorrection, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("400", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BlobUploadFails_ReturnsFailure500()
    {
        var issue = IssueFactory.CreateDefault();
        _repository.Setup(a => a.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _blobStorage.Setup(a => a.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(new Error("500", "Upload failed")));

        using var stream = new MemoryStream();
        var command = new AddPhotoCommand(issue.ID, "photo.jpg", "image/jpeg", stream, CorrectionStage.BeforeCorrection, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }

    [Fact]
    public async Task Handle_PresignedUrlFails_ReturnsFailure500()
    {
        var issue = IssueFactory.CreateDefault();
        _repository.Setup(a => a.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _blobStorage.Setup(a => a.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<string>("blob/key"));
        _blobStorage.Setup(a => a.GetPresignedUrlAsync("blob/key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(new Error("500", "URL failed")));
        _unitOfWork.Setup(a => a.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        using var stream = new MemoryStream();
        var command = new AddPhotoCommand(issue.ID, "photo.jpg", "image/jpeg", stream, CorrectionStage.BeforeCorrection, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }
}
