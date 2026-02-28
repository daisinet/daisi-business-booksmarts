using BookSmarts.Core.Encryption;

namespace BookSmarts.Core.Models;

public class CategorizationRule : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(CategorizationRule);
    public string CompanyId { get; set; } = "";
    public string? MerchantNameContains { get; set; }
    public string? PlaidCategory { get; set; }
    public string TargetAccountId { get; set; } = "";
    public string TargetAccountNumber { get; set; } = "";
    public string TargetAccountName { get; set; } = "";
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public int TimesApplied { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}
