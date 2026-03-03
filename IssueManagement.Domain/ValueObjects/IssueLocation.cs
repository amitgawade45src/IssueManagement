using IssueManagement.Domain.Enums;

namespace IssueManagement.Domain.ValueObjects;
 
// Represents the location of an issue in the 3D model.
// For Element issues: DbId is populated.
// For Spatial issues: WorldPosition is populated.
// Both can optionally be set.
// </summary>
public sealed class IssueLocation
{
    public LocationType LocationType { get; private set; }
    public int? DbId { get; private set; }    // The BIM element identifier (dbId) for element-based issues. 
    public WorldPosition? WorldPosition { get; private set; }    // The 3D world-coordinate position for spatial issues.
    private IssueLocation() { }
    public static IssueLocation CreateElementLocation(int dbId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dbId);
        return new IssueLocation
        {
            LocationType = LocationType.Element,
            DbId = dbId
        };
    } 
    public static IssueLocation CreateSpatialLocation(WorldPosition worldPosition)
    {
        ArgumentNullException.ThrowIfNull(worldPosition);
        return new IssueLocation
        {
            LocationType = LocationType.Spatial,
            WorldPosition = worldPosition
        };
    } 
    public static IssueLocation CreateCombinedLocation(int dbId, WorldPosition worldPosition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dbId);
        ArgumentNullException.ThrowIfNull(worldPosition);
        return new IssueLocation
        {
            LocationType = LocationType.ElementSpatial,
            DbId = dbId,
            WorldPosition = worldPosition
        };
    }
}
