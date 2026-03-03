using IssueManagement.Application.Abstractions;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Infrastructure.Persistence;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Reflection;

namespace IssueManagement.ArchitectureTests.Infrastructure;

public abstract class BaseTest
{
    protected static readonly Assembly ApplicationAssembly = typeof(IUnitOfWork).Assembly;

    protected static readonly Assembly DomainAssembly = typeof(Entity).Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(IssueDbContext).Assembly;

    protected static readonly Assembly PresentationAssembly = typeof(Program).Assembly;
}

