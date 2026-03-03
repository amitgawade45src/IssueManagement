namespace IssueManagement.Infrastructure.DTO.DBSets; 
public sealed class IssueModel : DbSetBase
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Type { get; set; }          // stored as string enum name
    public required string Status { get; set; }     // stored as string enum name 
    public required string LocationType { get; set; }   // "Element", "Spatial", "ElementSpatial"
    public int? LocationDbId { get; set; }
    public double? LocationWorldX { get; set; }
    public double? LocationWorldY { get; set; }
    public double? LocationWorldZ { get; set; } 
    // Navigation
    public List<IssuePhotoModel>? Photos { get; set; } = [];
    public List<IssueStatusHistoryModel>? StatusHistory { get; set; } = [];
}
