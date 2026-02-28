using BookSmarts.Core.Models;
using System.Text;

namespace BookSmarts.Services;

/// <summary>
/// Formats financial report models into compact markdown strings for AI inference context.
/// Pure formatting utility — no external dependencies.
/// </summary>
public class FinancialContextBuilder
{
    public string BuildBalanceSheetContext(BalanceSheetReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Balance Sheet ({(report.CashBasis ? "Cash" : "Accrual")} Basis)");
        sb.AppendLine($"As of {report.AsOfDate:yyyy-MM-dd}");
        sb.AppendLine();

        AppendSections(sb, "Assets", report.AssetSections);
        sb.AppendLine($"**Total Assets: {report.TotalAssets:N2}**");
        sb.AppendLine();

        AppendSections(sb, "Liabilities", report.LiabilitySections);
        sb.AppendLine($"**Total Liabilities: {report.TotalLiabilities:N2}**");
        sb.AppendLine();

        AppendSections(sb, "Equity", report.EquitySections);
        sb.AppendLine($"Retained Earnings: {report.RetainedEarnings:N2}");
        sb.AppendLine($"**Total Equity: {report.TotalEquity:N2}**");
        sb.AppendLine();

        sb.AppendLine($"**Total Liabilities & Equity: {report.TotalLiabilitiesAndEquity:N2}**");
        sb.AppendLine(report.IsBalanced ? "Balance sheet is in balance." : $"OUT OF BALANCE by {Math.Abs(report.TotalAssets - report.TotalLiabilitiesAndEquity):N2}");

        return sb.ToString();
    }

    public string BuildIncomeStatementContext(IncomeStatementReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Income Statement ({(report.CashBasis ? "Cash" : "Accrual")} Basis)");
        sb.AppendLine($"Period: {report.FromDate:yyyy-MM-dd} to {report.ToDate:yyyy-MM-dd}");
        sb.AppendLine();

        AppendSections(sb, "Revenue", report.RevenueSections);
        sb.AppendLine($"**Total Revenue: {report.TotalRevenue:N2}**");
        sb.AppendLine();

        AppendSections(sb, "Expenses", report.ExpenseSections);
        sb.AppendLine($"**Total Expenses: {report.TotalExpenses:N2}**");
        sb.AppendLine();

        sb.AppendLine($"**Net Income: {report.NetIncome:N2}**");

        return sb.ToString();
    }

    public string BuildCashFlowContext(CashFlowReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Statement of Cash Flows");
        sb.AppendLine($"Period: {report.FromDate:yyyy-MM-dd} to {report.ToDate:yyyy-MM-dd}");
        sb.AppendLine();

        sb.AppendLine("## Operating Activities");
        foreach (var item in report.OperatingActivities)
            sb.AppendLine($"  {item.Label}: {item.Amount:N2}");
        sb.AppendLine($"**Net Operating: {report.NetOperating:N2}**");
        sb.AppendLine();

        sb.AppendLine("## Investing Activities");
        foreach (var item in report.InvestingActivities)
            sb.AppendLine($"  {item.Label}: {item.Amount:N2}");
        sb.AppendLine($"**Net Investing: {report.NetInvesting:N2}**");
        sb.AppendLine();

        sb.AppendLine("## Financing Activities");
        foreach (var item in report.FinancingActivities)
            sb.AppendLine($"  {item.Label}: {item.Amount:N2}");
        sb.AppendLine($"**Net Financing: {report.NetFinancing:N2}**");
        sb.AppendLine();

        sb.AppendLine($"Net Change in Cash: {report.NetChange:N2}");
        sb.AppendLine($"Beginning Cash: {report.BeginningCashBalance:N2}");
        sb.AppendLine($"**Ending Cash: {report.EndingCashBalance:N2}**");

        return sb.ToString();
    }

    public string BuildBudgetVsActualContext(BudgetVsActualReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Budget vs Actual — {report.BudgetName}");
        sb.AppendLine($"Period: {report.FromDate:yyyy-MM-dd} to {report.ToDate:yyyy-MM-dd}");
        sb.AppendLine();

        sb.AppendLine("## Revenue");
        foreach (var sect in report.RevenueSections)
        {
            sb.AppendLine($"### {sect.Title}");
            foreach (var line in sect.Lines)
                sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: Budget {line.BudgetedAmount:N2} | Actual {line.ActualAmount:N2} | Variance {line.Variance:N2} ({line.VariancePercent:N1}%)");
        }
        sb.AppendLine($"**Total Revenue: Budget {report.TotalBudgetedRevenue:N2} | Actual {report.TotalActualRevenue:N2}**");
        sb.AppendLine();

        sb.AppendLine("## Expenses");
        foreach (var sect in report.ExpenseSections)
        {
            sb.AppendLine($"### {sect.Title}");
            foreach (var line in sect.Lines)
                sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: Budget {line.BudgetedAmount:N2} | Actual {line.ActualAmount:N2} | Variance {line.Variance:N2} ({line.VariancePercent:N1}%)");
        }
        sb.AppendLine($"**Total Expenses: Budget {report.TotalBudgetedExpenses:N2} | Actual {report.TotalActualExpenses:N2}**");
        sb.AppendLine();

        sb.AppendLine($"**Net Income: Budget {report.BudgetedNetIncome:N2} | Actual {report.ActualNetIncome:N2} | Variance {report.NetIncomeVariance:N2} ({report.NetIncomeVariancePercent:N1}%)**");

        return sb.ToString();
    }

    public string BuildAgingContext(List<AgingReportSummary> summaries, List<AgingReportLine> lines, string type)
    {
        var sb = new StringBuilder();
        var label = type == "ar" ? "Accounts Receivable" : "Accounts Payable";
        var contactLabel = type == "ar" ? "Customer" : "Vendor";
        sb.AppendLine($"# {label} Aging Report");
        sb.AppendLine();

        sb.AppendLine($"| {contactLabel} | Current | 1-30 | 31-60 | 61-90 | 90+ | Total |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var s in summaries)
            sb.AppendLine($"| {s.ContactName} | {s.Current:N2} | {s.Days1To30:N2} | {s.Days31To60:N2} | {s.Days61To90:N2} | {s.Days90Plus:N2} | {s.Total:N2} |");

        var totalCurrent = summaries.Sum(s => s.Current);
        var total1To30 = summaries.Sum(s => s.Days1To30);
        var total31To60 = summaries.Sum(s => s.Days31To60);
        var total61To90 = summaries.Sum(s => s.Days61To90);
        var total90Plus = summaries.Sum(s => s.Days90Plus);
        var grandTotal = summaries.Sum(s => s.Total);
        sb.AppendLine($"| **Totals** | **{totalCurrent:N2}** | **{total1To30:N2}** | **{total31To60:N2}** | **{total61To90:N2}** | **{total90Plus:N2}** | **{grandTotal:N2}** |");

        return sb.ToString();
    }

    public string BuildProjectionContext(
        List<IncomeStatementReport> historicalStatements,
        BalanceSheetReport currentBalanceSheet,
        CashFlowReport currentCashFlow)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Historical Financial Data for Projections");
        sb.AppendLine();

        sb.AppendLine("## Monthly Income Statements (Last 6 Months)");
        foreach (var stmt in historicalStatements)
        {
            sb.AppendLine($"### {stmt.FromDate:MMM yyyy}");
            sb.AppendLine($"  Revenue: {stmt.TotalRevenue:N2} | Expenses: {stmt.TotalExpenses:N2} | Net Income: {stmt.NetIncome:N2}");
        }
        sb.AppendLine();

        sb.AppendLine("## Current Balance Sheet");
        sb.AppendLine($"  Assets: {currentBalanceSheet.TotalAssets:N2} | Liabilities: {currentBalanceSheet.TotalLiabilities:N2} | Equity: {currentBalanceSheet.TotalEquity:N2}");
        sb.AppendLine();

        sb.AppendLine("## Current Cash Flow");
        sb.AppendLine($"  Operating: {currentCashFlow.NetOperating:N2} | Investing: {currentCashFlow.NetInvesting:N2} | Financing: {currentCashFlow.NetFinancing:N2}");
        sb.AppendLine($"  Ending Cash: {currentCashFlow.EndingCashBalance:N2}");

        return sb.ToString();
    }

    private static void AppendSections(StringBuilder sb, string heading, List<FinancialStatementSection> sections)
    {
        sb.AppendLine($"## {heading}");
        foreach (var sect in sections)
        {
            sb.AppendLine($"### {sect.Title}");
            foreach (var line in sect.Lines)
                sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: {line.Balance:N2}");
            sb.AppendLine($"  Subtotal: {sect.Total:N2}");
        }
    }
}
