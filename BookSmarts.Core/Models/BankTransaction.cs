using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

public class BankTransaction : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(BankTransaction);
    public string CompanyId { get; set; } = "";
    public string BankConnectionId { get; set; } = "";
    public string PlaidAccountId { get; set; } = "";
    public string PlaidTransactionId { get; set; } = "";
    public DateTime TransactionDate { get; set; }
    public DateTime? AuthorizedDate { get; set; }
    public string? MerchantName { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public string? IsoCurrencyCode { get; set; }
    public List<string> PlaidCategories { get; set; } = new();
    public bool IsPending { get; set; }
    public BankTransactionStatus Status { get; set; } = BankTransactionStatus.Uncategorized;
    public string? CategorizedAccountId { get; set; }
    public string? CategorizedAccountNumber { get; set; }
    public string? CategorizedAccountName { get; set; }
    public string? JournalEntryId { get; set; }
    public string? MatchedRuleId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}
