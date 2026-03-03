using System.ComponentModel.DataAnnotations.Schema;

namespace IssueManagement.Infrastructure.DTO.DBSets;
 
public sealed class IssueStatusHistoryModel : DbSetBase
{
    public Guid IssueId { get; set; }
    public required string Status { get; set; }   
    public string? Comment { get; set; } 
    [ForeignKey("IssueId")]
    public IssueModel? Issue { get; set; }
}
