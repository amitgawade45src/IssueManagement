using IssueManagement.Application.Abstractions;
using IssueManagement.Application.DTOs;
using IssueManagement.Domain.Enums;

namespace IssueManagement.Application.UseCases.Issues.Commands;

public sealed record CreateIssueCommand(string Title, string Description, IssueType Type, LocationType LocationType, int? DbId, double? WorldX, double? WorldY, double? WorldZ, string? CreatedBy) : ICommand<IssueDto>;
