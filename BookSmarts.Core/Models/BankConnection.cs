using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

public class BankConnection : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(BankConnection);
    public string CompanyId { get; set; } = "";
    public string InstitutionId { get; set; } = "";
    public string InstitutionName { get; set; } = "";
    public string PlaidItemId { get; set; } = "";
    public string EncryptedAccessToken { get; set; } = "";
    public string? TransactionsCursor { get; set; }
    public BankConnectionStatus Status { get; set; } = BankConnectionStatus.Active;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public List<BankAccount> Accounts { get; set; } = new();
    public DateTime? LastSyncUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastWebhookUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}

public class BankAccount
{
    public string PlaidAccountId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? OfficialName { get; set; }
    public string Mask { get; set; } = "";
    public string PlaidType { get; set; } = "";
    public string? PlaidSubType { get; set; }
    public string? MappedCoaAccountId { get; set; }
    public string? MappedCoaAccountNumber { get; set; }
    public string? MappedCoaAccountName { get; set; }
    public decimal? CurrentBalance { get; set; }
    public decimal? AvailableBalance { get; set; }
    public bool IsEnabled { get; set; } = true;
}
