namespace IssueManagement.Domain.Enums;

public enum LocationType
{
    Element = 0,  /// Element-based issue location (linked to a BIM element via dbId).
    Spatial = 1, /// Spatial issue location (linked to a world-coordinate position in 3D).
    ElementSpatial = 2 //Combined location — both a BIM element (dbId) and a world-coordinate position.
}
