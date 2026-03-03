using IssueManagement.Application.Abstractions;

namespace IssueManagement.Application.UseCases.Issues.Commands;

public sealed record DeleteIssueCommand(Guid Id) : ICommand;
