namespace IssueManagement.Infrastructure.DTO;

/// Base class for all Infrastructure persistence models (DB entities).
/// No dependency on Domain layer. 
public abstract class DbSetBase
{
    public Guid ID { get; set; }
    public DateTime CreatedOn { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
}
