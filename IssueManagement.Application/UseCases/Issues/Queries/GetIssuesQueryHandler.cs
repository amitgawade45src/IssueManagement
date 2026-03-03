using IssueManagement.Application.Abstractions;
using IssueManagement.Application.DTOs;
using IssueManagement.Application.Mapping;
using IssueManagement.Domain.Abstractions;
using IssueManagement.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace IssueManagement.Application.UseCases.Issues.Queries;

internal sealed class GetIssuesQueryHandler(IIssueRepository _repository, ILogger<GetIssuesQueryHandler> _logger) : IQueryHandler<GetIssuesQuery, IReadOnlyList<IssueDto>>
{
    public async Task<Result<IReadOnlyList<IssueDto>>> Handle(GetIssuesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var issues = await _repository.GetAsync(request.StatusFilter, request.TypeFilter, cancellationToken);
            return Result.Success<IReadOnlyList<IssueDto>>(issues.Select(i => i.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all issues");
            return Result.Failure<IReadOnlyList<IssueDto>>(new Error("500", "Internal Error!"));
        }
    }
}
