namespace BookSmarts.Core.Models;

/// <summary>
/// Budget vs Actual comparison report. Not persisted.
/// </summary>
public class BudgetVsActualReport
{
    public string BudgetName { get; set; } = "";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<BudgetVsActualSection> RevenueSections { get; set; } = new();
    public List<BudgetVsActualSection> ExpenseSections { get; set; } = new();
    public decimal TotalBudgetedRevenue { get; set; }
    public decimal TotalActualRevenue { get; set; }
    public decimal TotalBudgetedExpenses { get; set; }
    public decimal TotalActualExpenses { get; set; }
    public decimal BudgetedNetIncome => TotalBudgetedRevenue - TotalBudgetedExpenses;
    public decimal ActualNetIncome => TotalActualRevenue - TotalActualExpenses;
    public decimal NetIncomeVariance => ActualNetIncome - BudgetedNetIncome;
    public decimal NetIncomeVariancePercent => BudgetedNetIncome != 0
        ? Math.Round(NetIncomeVariance / BudgetedNetIncome * 100, 2)
        : 0;
}

/// <summary>
/// A grouped section within a BvA report (e.g. "Sales Revenue", "Payroll").
/// </summary>
public class BudgetVsActualSection
{
    public string Title { get; set; } = "";
    public List<BudgetVsActualLine> Lines { get; set; } = new();
    public decimal TotalBudgeted => Lines.Sum(l => l.BudgetedAmount);
    public decimal TotalActual => Lines.Sum(l => l.ActualAmount);
    public decimal TotalVariance => Lines.Sum(l => l.Variance);
}

/// <summary>
/// A single account line in a BvA report.
/// </summary>
public class BudgetVsActualLine
{
    public string AccountId { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public decimal BudgetedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance => ActualAmount - BudgetedAmount;
    public decimal VariancePercent => BudgetedAmount != 0
        ? Math.Round(Variance / BudgetedAmount * 100, 2)
        : 0;
}
