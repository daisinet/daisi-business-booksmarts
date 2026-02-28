namespace BookSmarts.Core.Models;

/// <summary>
/// Records a single auditable action taken in the system.
/// </summary>
public class AuditEntry
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(AuditEntry);
    public string CompanyId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? EntityLabel { get; set; }
    public string? Description { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
