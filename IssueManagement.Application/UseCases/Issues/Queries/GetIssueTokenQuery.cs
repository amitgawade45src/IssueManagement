using IssueManagement.Application.Abstractions;

namespace IssueManagement.Application.UseCases.Issues.Queries;

public sealed record GetIssueTokenQuery() : IQuery<(string Token, int ExpiresIn)>;
