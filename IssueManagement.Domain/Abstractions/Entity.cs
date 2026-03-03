namespace IssueManagement.Domain.Abstractions;

public abstract class Entity
{
    protected Entity(Guid id,string createdBy) { ID = id; CreatedBy = createdBy; }

    protected Entity(Guid id, DateTime createdOn, string createdBy, DateTime? updatedOn, string? updatedBy, DateTime? deletedOn)
    {
        ID = id;
        CreatedOn = createdOn;
        CreatedBy = createdBy;
        UpdatedOn = updatedOn;
        UpdatedBy = updatedBy;
        DeletedOn = deletedOn;
    }
    public Guid ID { get; }
    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;
    public string CreatedBy { get; protected set; }
    public DateTime? UpdatedOn { get; protected set; }
    public string? UpdatedBy { get; protected set; }
    public DateTime? DeletedOn { get; protected set; }
}
