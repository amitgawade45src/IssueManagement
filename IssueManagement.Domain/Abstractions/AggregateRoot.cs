namespace IssueManagement.Domain.Abstractions;

public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id, string createdBy) : base(id, createdBy) { }

    protected AggregateRoot(Guid id, DateTime createdOn, string createdBy, DateTime? updatedOn, string? updatedBy, DateTime? deletedOn) : base(id, createdOn, createdBy, updatedOn, updatedBy, deletedOn) { }
}