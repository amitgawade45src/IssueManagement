using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;

namespace IssueManagement.Domain.Repositories;

public interface IIssueRepository
{
    Task<Issue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default); 
    Task<IReadOnlyList<Issue>> GetAsync(IssueStatus? status = null, IssueType? type = null, CancellationToken cancellationToken = default);
    Task AddAsync(Issue issue, CancellationToken cancellationToken = default);
    Task UpdateAsync(Issue issue, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
