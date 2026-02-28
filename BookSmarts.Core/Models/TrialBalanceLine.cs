using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

/// <summary>
/// Represents a single line in a trial balance report. Not persisted.
/// </summary>
public class TrialBalanceLine
{
    public string AccountId { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public AccountCategory Category { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
