using IssueManagement.Application.Abstractions;
using IssueManagement.Domain.Enums;

namespace IssueManagement.Application.UseCases.Issues.Commands;

public sealed record ChangeIssueStatusCommand(Guid Id, IssueStatus NewStatus, string ChangedBy, string? Comment) : ICommand;