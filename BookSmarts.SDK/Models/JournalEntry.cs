namespace BookSmarts.SDK.Models;

public class JournalEntry
{
    public string Id { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string? EntryNumber { get; set; }
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string? Memo { get; set; }
    public string Status { get; set; } = "Draft";
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }
    public List<JournalLine> Lines { get; set; } = [];
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string? CreatedBy { get; set; }
    public string? PostedBy { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? PostedUtc { get; set; }
    public DateTime? VoidedUtc { get; set; }
}

public class JournalLine
{
    public int LineNumber { get; set; }
    public string AccountId { get; set; } = "";
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}
