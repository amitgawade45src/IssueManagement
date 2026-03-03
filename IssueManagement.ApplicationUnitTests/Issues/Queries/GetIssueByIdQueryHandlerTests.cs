using IssueManagement.Application.Interfaces;
using IssueManagement.Application.UseCases.Issues.Queries;
using IssueManagement.ApplicationUnitTests.Helpers;
using IssueManagement.ApplicationUnitTests.Stubs;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using Moq;

namespace IssueManagement.ApplicationUnitTests.Issues.Queries;

public class GetIssueByIdQueryHandlerTests
{
    private readonly Mock<IIssueRepository> _repository;
    private readonly Mock<IBlobStorageService> _blobStorage;
    private readonly Mock<TestLogger<GetIssueByIdQueryHandler>> _logger;
    private readonly GetIssueByIdQueryHandler _handler;

    public GetIssueByIdQueryHandlerTests()
    {
        _repository = new Mock<IIssueRepository>();
        _blobStorage = new Mock<IBlobStorageService>();
        _logger = new Mock<TestLogger<GetIssueByIdQueryHandler>>();
        _handler = new GetIssueByIdQueryHandler(_repository.Object, _blobStorage.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ExistingIssue_ReturnsSuccessWithDto()
    {
        var issue = IssueFactory.CreateDefault();
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);

        var query = new GetIssueByIdQuery(issue.ID);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(issue.ID, result.Value.Id);
        Assert.Equal("Test Issue", result.Value.Title);
    }

    [Fact]
    public async Task Handle_IssueNotFound_ReturnsFailure()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Issue?)null);

        var query = new GetIssueByIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }

    [Fact]
    public async Task Handle_IssueWithPhotos_GeneratesPresignedUrls()
    {
        var issue = IssueFactory.CreateWithPhoto();
        var photo = issue.Photos.First();
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _blobStorage.Setup(b => b.GetPresignedUrlAsync(photo.BlobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<string>("https://presigned-url.com/photo"));

        var query = new GetIssueByIdQuery(issue.ID);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Photos);
        Assert.Equal("https://presigned-url.com/photo", result.Value.Photos[0].PresignedUrl);
    }

    [Fact]
    public async Task Handle_PresignedUrlFails_StillReturnsIssueWithoutUrl()
    {
        var issue = IssueFactory.CreateWithPhoto();
        var photo = issue.Photos.First();
        _repository.Setup(r => r.GetByIdAsync(issue.ID, It.IsAny<CancellationToken>())).ReturnsAsync(issue);
        _blobStorage.Setup(b => b.GetPresignedUrlAsync(photo.BlobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>(new Error("500", "Blob error")));

        var query = new GetIssueByIdQuery(issue.ID);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Photos);
        Assert.Null(result.Value.Photos[0].PresignedUrl);
    }

    [Fact]
    public async Task Handle_RepositoryThrows_ReturnsFailure()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var query = new GetIssueByIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }
}
