using IssueManagement.Infrastructure.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueManagement.Infrastructure.Persistence.Configurations.BaseConfiguration;

internal class BaseConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : DbSetBase
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(x => x.ID);
        builder.Property(x => x.ID).ValueGeneratedNever();
        builder.Property(x => x.CreatedOn).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.Property(x => x.CreatedBy).HasMaxLength(200);
        builder.Property(x => x.UpdatedBy).HasMaxLength(200);
    }
}