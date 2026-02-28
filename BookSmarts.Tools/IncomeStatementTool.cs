using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Retrieves the income statement (profit and loss) for a company over a date range.
/// </summary>
public class IncomeStatementTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";
    private const string P_FROM_DATE = "from-date";
    private const string P_TO_DATE = "to-date";
    private const string P_CASH_BASIS = "cash-basis";

    public override string Id => "booksmarts-income-statement";
    public override string Name => "BookSmarts Income Statement";

    public override string UseInstructions =>
        "Use this tool to get an income statement (profit and loss / P&L) for a company. " +
        "Shows revenue, expenses, and net income for a date range. " +
        "Keywords: income statement, profit and loss, P&L, revenue, expenses, net income, earnings.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID.", IsRequired = true },
        new() { Name = P_FROM_DATE, Description = "Start date (yyyy-MM-dd). Defaults to January 1 of this year.", IsRequired = false },
        new() { Name = P_TO_DATE, Description = "End date (yyyy-MM-dd). Defaults to today.", IsRequired = false },
        new() { Name = P_CASH_BASIS, Description = "true for cash basis, false for accrual. Defaults to false.", IsRequired = false }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;
        var fromStr = parameters.GetParameterValueOrDefault(P_FROM_DATE, null);
        var toStr = parameters.GetParameterValueOrDefault(P_TO_DATE, null);
        var cashBasisStr = parameters.GetParameterValueOrDefault(P_CASH_BASIS, "false");

        var fromDate = string.IsNullOrEmpty(fromStr) ? new DateTime(DateTime.Today.Year, 1, 1) : DateTime.Parse(fromStr);
        var toDate = string.IsNullOrEmpty(toStr) ? DateTime.Today : DateTime.Parse(toStr);
        var cashBasis = string.Equals(cashBasisStr, "true", StringComparison.OrdinalIgnoreCase);

        return new ToolExecutionContext
        {
            ExecutionMessage = $"Generating income statement {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
            ExecutionTask = Execute(toolContext, companyId, fromDate, toDate, cashBasis)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId, DateTime from, DateTime to, bool cashBasis)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<FinancialStatementService>();
            var report = await svc.GetIncomeStatementAsync(companyId, from, to, cashBasis);

            var sb = new StringBuilder();
            sb.AppendLine($"# Income Statement — {(cashBasis ? "Cash Basis" : "Accrual Basis")}");
            sb.AppendLine($"{from:MMMM d, yyyy} to {to:MMMM d, yyyy}");
            sb.AppendLine();

            sb.AppendLine("## Revenue");
            foreach (var section in report.RevenueSections)
            {
                sb.AppendLine($"### {section.Title}");
                foreach (var line in section.Lines)
                    sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: {line.Balance:C}");
                sb.AppendLine($"  **{section.Title} Total: {section.Total:C}**");
            }
            sb.AppendLine($"**Total Revenue: {report.TotalRevenue:C}**");
            sb.AppendLine();

            sb.AppendLine("## Expenses");
            foreach (var section in report.ExpenseSections)
            {
                sb.AppendLine($"### {section.Title}");
                foreach (var line in section.Lines)
                    sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: {line.Balance:C}");
                sb.AppendLine($"  **{section.Title} Total: {section.Total:C}**");
            }
            sb.AppendLine($"**Total Expenses: {report.TotalExpenses:C}**");
            sb.AppendLine();
            sb.AppendLine($"**Net Income: {report.NetIncome:C}**");

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"Income statement {from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
                OutputFormat = InferenceOutputFormats.Markdown,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
