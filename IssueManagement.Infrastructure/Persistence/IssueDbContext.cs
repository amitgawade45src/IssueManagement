using IssueManagement.Application.Abstractions;
using IssueManagement.Application.Exceptions;
using IssueManagement.Infrastructure.DTO.DBSets;
using Microsoft.EntityFrameworkCore;

namespace IssueManagement.Infrastructure.Persistence;

public class IssueDbContext(DbContextOptions<IssueDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<IssueModel> Issues => Set<IssueModel>();
    public DbSet<IssuePhotoModel> IssuePhotos => Set<IssuePhotoModel>();
    public DbSet<IssueStatusHistoryModel> IssueStatusHistories => Set<IssueStatusHistoryModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IssueDbContext).Assembly);
    }

    public sealed override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            throw new DatabaseUpdateException("Database update exception occurred.", ex);
        }
    }
}
