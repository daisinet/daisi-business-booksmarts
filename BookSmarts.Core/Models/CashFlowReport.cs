namespace BookSmarts.Core.Models;

/// <summary>
/// Statement of cash flows (indirect method) for a date range. Not persisted.
/// </summary>
public class CashFlowReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal BeginningCashBalance { get; set; }
    public List<CashFlowItem> OperatingActivities { get; set; } = new();
    public List<CashFlowItem> InvestingActivities { get; set; } = new();
    public List<CashFlowItem> FinancingActivities { get; set; } = new();
    public decimal NetOperating { get; set; }
    public decimal NetInvesting { get; set; }
    public decimal NetFinancing { get; set; }
    public decimal NetChange => NetOperating + NetInvesting + NetFinancing;
    public decimal EndingCashBalance => BeginningCashBalance + NetChange;
}

/// <summary>
/// A single line item within a cash flow section.
/// </summary>
public class CashFlowItem
{
    public string Label { get; set; } = "";
    public string? AccountNumber { get; set; }
    public decimal Amount { get; set; }
}
