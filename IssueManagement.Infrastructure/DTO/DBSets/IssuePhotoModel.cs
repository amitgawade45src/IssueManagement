using System.ComponentModel.DataAnnotations.Schema;

namespace IssueManagement.Infrastructure.DTO.DBSets;

public sealed class IssuePhotoModel : DbSetBase
{
    public Guid IssueId { get; set; }
    public required string BlobKey { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string CorrectionStage { get; set; } 
    [ForeignKey("IssueId")]
    public IssueModel? Issue { get; set; }
}
