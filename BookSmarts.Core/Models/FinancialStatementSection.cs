using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

/// <summary>
/// A grouped section within a financial statement (e.g. "Current Assets", "Long-Term Liabilities").
/// </summary>
public class FinancialStatementSection
{
    public string Title { get; set; } = "";
    public AccountSubType? SubType { get; set; }
    public List<FinancialStatementLine> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(l => l.Balance);
}

/// <summary>
/// A single account line within a financial statement section.
/// </summary>
public class FinancialStatementLine
{
    public string AccountId { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public decimal Balance { get; set; }
}
