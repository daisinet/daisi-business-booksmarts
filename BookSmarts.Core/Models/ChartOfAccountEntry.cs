using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

public class ChartOfAccountEntry : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(ChartOfAccountEntry);
    public string CompanyId { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public AccountCategory Category { get; set; }
    public AccountSubType SubType { get; set; }
    public NormalBalance NormalBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystemAccount { get; set; }
    public string? ParentAccountId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}
