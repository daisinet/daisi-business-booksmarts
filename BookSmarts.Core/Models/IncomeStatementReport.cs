namespace BookSmarts.Core.Models;

/// <summary>
/// Income statement (profit &amp; loss) report for a date range. Not persisted.
/// </summary>
public class IncomeStatementReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public bool CashBasis { get; set; }
    public List<FinancialStatementSection> RevenueSections { get; set; } = new();
    public List<FinancialStatementSection> ExpenseSections { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome => TotalRevenue - TotalExpenses;
}
