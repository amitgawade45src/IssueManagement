using IssueManagement.Application.Abstractions;
using IssueManagement.Application.DTOs;
using IssueManagement.Domain.Enums;

namespace IssueManagement.Application.UseCases.Issues.Queries;

public sealed record GetIssuesQuery(IssueStatus? StatusFilter = null, IssueType? TypeFilter = null) : IQuery<IReadOnlyList<IssueDto>>;
