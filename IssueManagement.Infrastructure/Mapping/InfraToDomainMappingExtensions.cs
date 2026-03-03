using IssueManagement.Domain.Enums;
using IssueManagement.Domain.Models;
using IssueManagement.Domain.ValueObjects;
using IssueManagement.Infrastructure.DTO.DBSets;

namespace IssueManagement.Infrastructure.Mapping;

//  Maps between Domain models and Infrastructure persistence models. automapper can be used in future. 

internal static class InfraToDomainMappingExtensions
{
    public static IssueModel ToDbModel(this Issue issue)
    {
        return new IssueModel
        {
            ID = issue.ID,
            Title = issue.Title.Value,
            Description = issue.Description.Value,
            Type = issue.Type.ToString(),
            Status = issue.Status.ToString(),
            LocationType = issue.Location.LocationType.ToString(),
            LocationDbId = issue.Location.DbId,
            LocationWorldX = issue.Location.WorldPosition?.X,
            LocationWorldY = issue.Location.WorldPosition?.Y,
            LocationWorldZ = issue.Location.WorldPosition?.Z,
            CreatedOn = issue.CreatedOn,
            CreatedBy = issue.CreatedBy,
            UpdatedOn = issue.UpdatedOn,
            UpdatedBy = issue.UpdatedBy,
            DeletedOn = issue.DeletedOn,
            Photos = issue.Photos.Select(p => p.ToDbModel()).ToList(),
            StatusHistory = issue.StatusHistory.Select(h => h.ToDbModel()).ToList()
        };
    }

    public static IssuePhotoModel ToDbModel(this IssuePhoto photo)
    {
        return new IssuePhotoModel
        {
            ID = photo.ID,
            IssueId = photo.IssueId,
            BlobKey = photo.BlobKey,
            FileName = photo.FileName,
            ContentType = photo.ContentType,
            CorrectionStage = photo.CorrectionStage.ToString(),
            CreatedOn = photo.CreatedOn,
            CreatedBy = photo.CreatedBy,
            UpdatedOn = photo.UpdatedOn,
            UpdatedBy = photo.UpdatedBy
        };
    }

    public static IssueStatusHistoryModel ToDbModel(this IssueStatusHistory history)
    {
        return new IssueStatusHistoryModel
        {
            ID = history.ID,
            IssueId = history.IssueId,
            Status = history.Status.ToString(),
            Comment = history.Comment,
            CreatedOn = history.CreatedOn,
            CreatedBy = history.CreatedBy,
            UpdatedOn = history.UpdatedOn,
            UpdatedBy = history.UpdatedBy
        };
    }


    public static Issue ToDomain(this IssueModel model)
    {
        var title = IssueTitle.Create(model.Title);
        var description = IssueDescription.Create(model.Description);
        var type = Enum.Parse<IssueType>(model.Type);
        var status = Enum.Parse<IssueStatus>(model.Status);
        var location = BuildLocation(model);

        var photos = model.Photos!.Select(p => p.ToDomain()).ToList();
        var statusHistory = model.StatusHistory!.Select(h => h.ToDomain()).ToList();

        return Issue.Reconstitute(
            model.ID,
            title,
            description,
            type,
            status,
            location,
            model.CreatedOn,
            model.CreatedBy,
            model.UpdatedOn,
            model.UpdatedBy,
            model.DeletedOn,
            photos,
            statusHistory);
    }

    public static IssuePhoto ToDomain(this IssuePhotoModel model)
    {
        var correctionStage = Enum.Parse<CorrectionStage>(model.CorrectionStage);
        return IssuePhoto.Reconstitute(
            model.ID,
            model.IssueId,
            model.BlobKey,
            model.FileName,
            model.ContentType,
            correctionStage,
            model.CreatedOn,
            model.CreatedBy);
    }

    public static IssueStatusHistory ToDomain(this IssueStatusHistoryModel model)
    {
        var status = Enum.Parse<IssueStatus>(model.Status);
        return IssueStatusHistory.Reconstitute(
            model.ID,
            model.IssueId,
            status,
            model.CreatedOn,
            model.CreatedBy,
            model.UpdatedBy,
            model.Comment,
            model.UpdatedOn);
    }

    private static IssueLocation BuildLocation(IssueModel model)
    {
        var locationType = Enum.Parse<LocationType>(model.LocationType);
        var hasDbId = model.LocationDbId.HasValue;
        var hasWorld = model.LocationWorldX.HasValue && model.LocationWorldY.HasValue && model.LocationWorldZ.HasValue;

        return (locationType, hasDbId, hasWorld) switch
        {
            (LocationType.ElementSpatial, true, true) =>
                IssueLocation.CreateCombinedLocation(model.LocationDbId!.Value,
                    new WorldPosition(model.LocationWorldX!.Value, model.LocationWorldY!.Value, model.LocationWorldZ!.Value)),
            (LocationType.Element, true, _) =>
                IssueLocation.CreateElementLocation(model.LocationDbId!.Value),
            (LocationType.Spatial, _, true) =>
                IssueLocation.CreateSpatialLocation(
                    new WorldPosition(model.LocationWorldX!.Value, model.LocationWorldY!.Value, model.LocationWorldZ!.Value)),
            _ => throw new InvalidOperationException($"Invalid location data in database for issue {model.ID}")
        };
    }
}
