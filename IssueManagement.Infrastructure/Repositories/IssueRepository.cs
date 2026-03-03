using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.Repositories;
using IssueManagement.Infrastructure.DTO.DBSets;
using IssueManagement.Infrastructure.Mapping;
using IssueManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IssueManagement.Infrastructure.Repositories;

internal sealed class IssueRepository(IssueDbContext dbContext) : IIssueRepository
{
    public async Task<Issue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Issues
            .Include(i => i.Photos)
            .Include(i => i.StatusHistory)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ID == id, cancellationToken);

        return model?.ToDomain();
    }

    public async Task<IReadOnlyList<Issue>> GetAsync(IssueStatus? status = null, IssueType? type = null, CancellationToken cancellationToken = default)
    {
        IQueryable<IssueModel> query = dbContext.Issues
            .Include(i => i.Photos)
            .Include(i => i.StatusHistory)
            .AsNoTracking();

        if (status.HasValue)
        {
            var statusStr = status.Value.ToString();
            query = query.Where(i => i.Status == statusStr);
        }

        if (type.HasValue)
        {
            var typeStr = type.Value.ToString();
            query = query.Where(i => i.Type == typeStr);
        }

        var models = await query
            .OrderByDescending(i => i.CreatedOn)
            .ToListAsync(cancellationToken);

        return models.Select(m => m.ToDomain()).ToList();
    }
    public async Task AddAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        var model = issue.ToDbModel();
        await dbContext.Issues.AddAsync(model, cancellationToken);
    }

    public async Task UpdateAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        // Load the existing tracked entity
        var existing = await dbContext.Issues
            .Include(i => i.Photos)
            .Include(i => i.StatusHistory)
            .FirstOrDefaultAsync(i => i.ID == issue.ID, cancellationToken)
            ?? throw new KeyNotFoundException($"Issue {issue.ID} not found.");

        var updated = issue.ToDbModel();

        // Update scalar properties
        dbContext.Entry(existing).CurrentValues.SetValues(updated);

        // Sync Photos collection
        SyncCollection(existing.Photos!, updated.Photos!, p => p.ID);

        // Sync StatusHistory collection (append-only, but handles full sync)
        SyncCollection(existing.StatusHistory!, updated.StatusHistory!, h => h.ID);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Issues.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException($"Issue {id} not found.");

        dbContext.Issues.Remove(model);
    }

    /// <summary>
    /// Syncs a child collection: adds new items, updates existing, removes deleted.
    /// </summary>
    private void SyncCollection<T>(List<T> existing, List<T> updated, Func<T, Guid> keySelector)
        where T : class
    {
        var existingIds = existing.Select(keySelector).ToHashSet();
        var updatedIds = updated.Select(keySelector).ToHashSet();

        // Remove items not in updated
        var toRemove = existing.Where(e => !updatedIds.Contains(keySelector(e))).ToList();
        foreach (var item in toRemove)
        {
            existing.Remove(item);
            dbContext.Remove(item);
        }

        // Add new items
        foreach (var item in updated.Where(u => !existingIds.Contains(keySelector(u))))
        {
            existing.Add(item);
        }

        // Update existing items
        foreach (var item in updated.Where(u => existingIds.Contains(keySelector(u))))
        {
            var existingItem = existing.First(e => keySelector(e) == keySelector(item));
            dbContext.Entry(existingItem).CurrentValues.SetValues(item);
        }
    }
}
