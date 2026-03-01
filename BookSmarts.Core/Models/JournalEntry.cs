using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

public class JournalEntry : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(JournalEntry);
    public string CompanyId { get; set; } = "";
    public string EntryNumber { get; set; } = "";
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string? Memo { get; set; }
    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
    public SourceType SourceType { get; set; } = SourceType.Manual;
    public string? SourceId { get; set; }
    public string? ReversalOfId { get; set; }
    public string? FiscalPeriodId { get; set; }
    public List<JournalLine> Lines { get; set; } = new();
    public decimal TotalDebit => Lines?.Sum(l => l.Debit) ?? 0;
    public decimal TotalCredit => Lines?.Sum(l => l.Credit) ?? 0;
    public bool IsBalanced => TotalDebit == TotalCredit && TotalDebit > 0;
    public string? CreatedBy { get; set; }
    public string? PostedBy { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PostedUtc { get; set; }
    public DateTime? VoidedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}

public class JournalLine
{
    public int LineNumber { get; set; }
    public string AccountId { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}
