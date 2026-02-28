namespace BookSmarts.Core.Models;

/// <summary>
/// Consolidated income statement across all companies in an organization. Not persisted.
/// </summary>
public class ConsolidatedIncomeStatementReport
{
    public string OrganizationName { get; set; } = "";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public bool CashBasis { get; set; }
    public List<CompanyIncomeStatement> CompanyReports { get; set; } = new();
    public IncomeStatementReport Consolidated { get; set; } = new();
    public IncomeStatementReport? Eliminations { get; set; }
}

/// <summary>
/// A single company's income statement within a consolidated report.
/// </summary>
public class CompanyIncomeStatement
{
    public string CompanyId { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public IncomeStatementReport Report { get; set; } = new();
}
