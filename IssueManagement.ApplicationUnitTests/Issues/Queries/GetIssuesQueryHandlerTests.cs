using IssueManagement.Application.UseCases.Issues.Queries;
using IssueManagement.ApplicationUnitTests.Helpers;
using IssueManagement.ApplicationUnitTests.Stubs;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using Moq;

namespace IssueManagement.ApplicationUnitTests.Issues.Queries;

public class GetIssuesQueryHandlerTests
{
    private readonly Mock<IIssueRepository> _repository;
    private readonly Mock<TestLogger<GetIssuesQueryHandler>> _logger;
    private readonly GetIssuesQueryHandler _handler;

    public GetIssuesQueryHandlerTests()
    {
        _repository = new Mock<IIssueRepository>();
        _logger = new Mock<TestLogger<GetIssuesQueryHandler>>();
        _handler = new GetIssuesQueryHandler(_repository.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_NoFilter_ReturnsAllIssues()
    {
        var issues = new List<Issue>
        {
            IssueFactory.CreateDefault(title: "Issue1"),
            IssueFactory.CreateDefault(title: "Issue2")
        };
        _repository.Setup(r => r.GetAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(issues.AsReadOnly());

        var query = new GetIssuesQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_PassesFilterToRepository()
    {
        _repository.Setup(r => r.GetAsync(IssueStatus.Open, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Issue>().AsReadOnly());

        var query = new GetIssuesQuery(StatusFilter: IssueStatus.Open);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.GetAsync(IssueStatus.Open, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTypeFilter_PassesFilterToRepository()
    {
        _repository.Setup(r => r.GetAsync(null, IssueType.SafetyDeficiency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Issue>().AsReadOnly());

        var query = new GetIssuesQuery(TypeFilter: IssueType.SafetyDeficiency);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.GetAsync(null, IssueType.SafetyDeficiency, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryThrows_ReturnsFailure500()
    {
        _repository.Setup(r => r.GetAsync(It.IsAny<IssueStatus?>(), It.IsAny<IssueType?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var query = new GetIssuesQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        _repository.Setup(r => r.GetAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Issue>().AsReadOnly());

        var query = new GetIssuesQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
