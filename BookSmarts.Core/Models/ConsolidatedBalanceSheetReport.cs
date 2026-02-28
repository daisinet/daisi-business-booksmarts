namespace BookSmarts.Core.Models;

/// <summary>
/// Consolidated balance sheet across all companies in an organization. Not persisted.
/// </summary>
public class ConsolidatedBalanceSheetReport
{
    public string OrganizationName { get; set; } = "";
    public DateTime AsOfDate { get; set; }
    public bool CashBasis { get; set; }
    public List<CompanyBalanceSheet> CompanyReports { get; set; } = new();
    public BalanceSheetReport Consolidated { get; set; } = new();
    public BalanceSheetReport? Eliminations { get; set; }
}

/// <summary>
/// A single company's balance sheet within a consolidated report.
/// </summary>
public class CompanyBalanceSheet
{
    public string CompanyId { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public BalanceSheetReport Report { get; set; } = new();
}
