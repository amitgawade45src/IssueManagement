using IssueManagement.Application.Interfaces;
using IssueManagement.Domain.Repositories;
using IssueManagement.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace IssueManagement.Infrastructure.IntegrationTests.Infrastructure;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
{
    protected readonly IServiceScope _scope;
    protected readonly IIssueRepository _issueRepository;
    protected readonly IBlobStorageService _blobStorageService;
    protected readonly IssueDbContext _dbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _issueRepository = _scope.ServiceProvider.GetRequiredService<IIssueRepository>();
        _blobStorageService = _scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
        _dbContext = _scope.ServiceProvider.GetRequiredService<IssueDbContext>();
    }
}
