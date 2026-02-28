using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

/// <summary>
/// A saved custom report definition that can be run on demand.
/// </summary>
public class CustomReport
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(CustomReport);
    public string CompanyId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public CustomReportType ReportType { get; set; } = CustomReportType.AccountBalances;

    /// <summary>
    /// Account categories to include. Empty means all.
    /// </summary>
    public List<AccountCategory> Categories { get; set; } = new();

    /// <summary>
    /// Specific account sub-types to include. Empty means all.
    /// </summary>
    public List<AccountSubType> SubTypes { get; set; } = new();

    /// <summary>
    /// Specific account IDs to include. Empty means all matching categories/sub-types.
    /// </summary>
    public List<string> AccountIds { get; set; } = new();

    /// <summary>
    /// How to group the results.
    /// </summary>
    public CustomReportGrouping Grouping { get; set; } = CustomReportGrouping.Category;

    /// <summary>
    /// Whether to show zero-balance accounts.
    /// </summary>
    public bool ShowZeroBalances { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}

/// <summary>
/// Result of running a custom report.
/// </summary>
public class CustomReportResult
{
    public string ReportName { get; set; } = "";
    public CustomReportType ReportType { get; set; }
    public DateTime? AsOfDate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool CashBasis { get; set; }
    public List<CustomReportGroup> Groups { get; set; } = new();
    public decimal GrandTotal { get; set; }
}

public class CustomReportGroup
{
    public string Title { get; set; } = "";
    public List<CustomReportLine> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(l => l.Balance);
}

public class CustomReportLine
{
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public AccountCategory Category { get; set; }
    public AccountSubType SubType { get; set; }
    public decimal Balance { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
