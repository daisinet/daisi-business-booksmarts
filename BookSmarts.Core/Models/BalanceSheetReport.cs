namespace BookSmarts.Core.Models;

/// <summary>
/// Balance sheet report — Assets = Liabilities + Equity. Not persisted.
/// </summary>
public class BalanceSheetReport
{
    public DateTime AsOfDate { get; set; }
    public bool CashBasis { get; set; }
    public List<FinancialStatementSection> AssetSections { get; set; } = new();
    public List<FinancialStatementSection> LiabilitySections { get; set; } = new();
    public List<FinancialStatementSection> EquitySections { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal RetainedEarnings { get; set; }
    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public bool IsBalanced => TotalAssets == TotalLiabilitiesAndEquity;
}
