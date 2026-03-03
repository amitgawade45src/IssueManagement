using IssueManagement.Application.Abstractions;
using IssueManagement.Application.DTOs;

namespace IssueManagement.Application.UseCases.Issues.Queries;

public sealed record GetIssueByIdQuery(Guid Id) : IQuery<IssueDto?>;
