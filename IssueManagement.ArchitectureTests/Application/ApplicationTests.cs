using IssueManagement.Application.Abstractions;
using IssueManagement.ArchitectureTests.Infrastructure;
using NetArchTest.Rules;

namespace IssueManagement.ArchitectureTests.Application;

public class ApplicationTests : BaseTest
{
    [Fact]
    public void CommandHandler_ShouldHave_NameEndingWith_CommandHandler()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
             .That()
             .ImplementInterface(typeof(ICommandHandler<>))
             .Or()
             .ImplementInterface(typeof(ICommandHandler<,>))
             .And()
             .AreNotAbstract()
             .Should()
             .HaveNameEndingWith("CommandHandler")
             .GetResult();

        Assert.True(result.IsSuccessful, "Some command handlers do not follow the naming convention.");
    }

    [Fact]
    public void CommandHandler_Should_NotBePublic()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .And()
            .AreNotAbstract()
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, "Some command handlers are public.");
    }
    [Fact]
    public void QueryHandler_ShouldHave_NameEndingWith_QueryHandler()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("QueryHandler")
            .GetResult();

        Assert.True(result.IsSuccessful, "Some query handlers do not follow the naming convention.");
    }

    [Fact]
    public void QueryHandler_Should_NotBePublic()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .And()
            .AreNotAbstract()
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, "Some query handlers are public.");
    }
}
