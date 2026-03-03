using IssueManagement.Infrastructure.DTO.DBSets;
using IssueManagement.Infrastructure.Persistence.Configurations.BaseConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueManagement.Infrastructure.Persistence.Configurations;

internal class IssueModelConfiguration : BaseConfiguration<IssueModel>
{
    public override void Configure(EntityTypeBuilder<IssueModel> builder)
    {
        base.Configure(builder);
        builder.ToTable("Issues");
        builder.Property(i => i.Title).HasMaxLength(200);
        builder.Property(i => i.Description).HasMaxLength(4000);
        builder.Property(i => i.Type).HasMaxLength(50);
        builder.Property(i => i.Status).HasMaxLength(20);
        builder.Property(i => i.LocationType).HasMaxLength(20);
        builder.Property(i => i.LocationDbId);
        builder.Property(i => i.LocationWorldX).HasColumnType("float");
        builder.Property(i => i.LocationWorldY).HasColumnType("float");
        builder.Property(i => i.LocationWorldZ).HasColumnType("float");
        builder.HasMany(i => i.Photos)
            .WithOne(p => p.Issue)
            .HasForeignKey(p => p.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.StatusHistory)
            .WithOne(h => h.Issue)
            .HasForeignKey(h => h.IssueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class IssuePhotoModelConfiguration : BaseConfiguration<IssuePhotoModel>
{
    public override void Configure(EntityTypeBuilder<IssuePhotoModel> builder)
    {
        base.Configure(builder);
        builder.ToTable("IssuePhotos");
        builder.Property(p => p.BlobKey).HasMaxLength(500);
        builder.Property(p => p.FileName).HasMaxLength(260);
        builder.Property(p => p.ContentType).HasMaxLength(100);
        builder.Property(p => p.CorrectionStage).HasMaxLength(30); 
    }
}

internal class IssueStatusHistoryModelConfiguration : BaseConfiguration<IssueStatusHistoryModel>
{
    public override void Configure(EntityTypeBuilder<IssueStatusHistoryModel> builder)
    {
        base.Configure(builder);
        builder.ToTable("IssueStatusHistories");
        builder.Property(h => h.Status).HasMaxLength(20);
        builder.Property(h => h.Comment).HasMaxLength(1000);
    }
}
