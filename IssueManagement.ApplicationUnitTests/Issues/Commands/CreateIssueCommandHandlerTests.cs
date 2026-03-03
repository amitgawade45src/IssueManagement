using IssueManagement.Application.Abstractions;
using IssueManagement.Application.UseCases.Issues.Commands;
using IssueManagement.ApplicationUnitTests.Stubs;
using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using Moq;

namespace IssueManagement.ApplicationUnitTests.Issues.Commands;

public class CreateIssueCommandHandlerTests
{
    private readonly Mock<IIssueRepository> _repository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<TestLogger<CreateIssueCommandHandler>> _logger;
    private readonly CreateIssueCommandHandler _handler;

    public CreateIssueCommandHandlerTests()
    {
        _repository = new Mock<IIssueRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<TestLogger<CreateIssueCommandHandler>>();
        _handler = new CreateIssueCommandHandler(_repository.Object, _unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithIssueDto()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateIssueCommand("Title", "Desc", IssueType.QualityDefect, LocationType.Element, 42, null, null, null, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Title", result.Value.Title);
        Assert.Equal("Desc", result.Value.Description);
        Assert.Equal(IssueType.QualityDefect, result.Value.Type);
        Assert.Equal(IssueStatus.Open, result.Value.Status);
        _repository.Verify(r => r.AddAsync(It.IsAny<Issue>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SaveReturnsZero_ReturnsFailure()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var command = new CreateIssueCommand("Title", "Desc", IssueType.QualityDefect, LocationType.Element, 42, null, null, null, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SpatialLocation_ReturnsSuccessWithSpatialData()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateIssueCommand("Title", "Desc", IssueType.SafetyDeficiency, LocationType.Spatial, null, 1.0, 2.0, 3.0, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LocationType.Spatial, result.Value.Location.LocationType);
        Assert.NotNull(result.Value.Location.WorldPosition);
    }

    [Fact]
    public async Task Handle_CombinedLocation_ReturnsSuccessWithBothDbIdAndWorldPosition()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateIssueCommand("Title", "Desc", IssueType.DesignChange, LocationType.ElementSpatial, 10, 1.0, 2.0, 3.0, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LocationType.ElementSpatial, result.Value.Location.LocationType);
        Assert.Equal(10, result.Value.Location.DbId);
        Assert.NotNull(result.Value.Location.WorldPosition);
    }

    [Fact]
    public async Task Handle_InvalidLocationData_ReturnsFailure()
    {
        var command = new CreateIssueCommand("Title", "Desc", IssueType.QualityDefect, LocationType.Element, null, null, null, null, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("500", result.Error.Code);
    }

    [Fact]
    public async Task Handle_RepositoryThrows_ReturnsFailure()
    {
        _repository.Setup(r => r.AddAsync(It.IsAny<Issue>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var command = new CreateIssueCommand("Title", "Desc", IssueType.QualityDefect, LocationType.Element, 42, null, null, null, "user");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}
