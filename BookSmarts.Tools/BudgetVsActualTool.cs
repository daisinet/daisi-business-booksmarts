using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Retrieves a budget vs actual comparison report for a company.
/// </summary>
public class BudgetVsActualTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";
    private const string P_BUDGET_ID = "budget-id";
    private const string P_FROM_DATE = "from-date";
    private const string P_TO_DATE = "to-date";
    private const string P_CASH_BASIS = "cash-basis";

    public override string Id => "booksmarts-budget-vs-actual";
    public override string Name => "BookSmarts Budget vs Actual";

    public override string UseInstructions =>
        "Use this tool to compare budgeted amounts to actual results for a company. " +
        "Shows revenue and expense variances by account. Requires a budget to exist. " +
        "Keywords: budget, actual, variance, over budget, under budget, spending, budget comparison.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID to get the budget comparison for.", IsRequired = true },
        new() { Name = P_BUDGET_ID, Description = "The budget ID to compare against. If not provided, uses the first available budget.", IsRequired = false },
        new() { Name = P_FROM_DATE, Description = "Start date for the period (yyyy-MM-dd). Defaults to start of current year.", IsRequired = false },
        new() { Name = P_TO_DATE, Description = "End date for the period (yyyy-MM-dd). Defaults to today.", IsRequired = false },
        new() { Name = P_CASH_BASIS, Description = "true for cash basis, false for accrual basis. Defaults to false.", IsRequired = false }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;
        var budgetId = parameters.GetParameterValueOrDefault(P_BUDGET_ID, null);
        var fromDateStr = parameters.GetParameterValueOrDefault(P_FROM_DATE, null);
        var toDateStr = parameters.GetParameterValueOrDefault(P_TO_DATE, null);
        var cashBasisStr = parameters.GetParameterValueOrDefault(P_CASH_BASIS, "false");

        var fromDate = string.IsNullOrEmpty(fromDateStr) ? new DateTime(DateTime.Today.Year, 1, 1) : DateTime.Parse(fromDateStr);
        var toDate = string.IsNullOrEmpty(toDateStr) ? DateTime.Today : DateTime.Parse(toDateStr);
        var cashBasis = string.Equals(cashBasisStr, "true", StringComparison.OrdinalIgnoreCase);

        return new ToolExecutionContext
        {
            ExecutionMessage = $"Generating budget vs actual report",
            ExecutionTask = Execute(toolContext, companyId, budgetId, fromDate, toDate, cashBasis)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId, string? budgetId, DateTime fromDate, DateTime toDate, bool cashBasis)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<BudgetService>();

            // If no budget ID provided, use the first available budget
            if (string.IsNullOrEmpty(budgetId))
            {
                var budgets = await svc.GetBudgetsAsync(companyId);
                if (budgets.Count == 0)
                    return new ToolResult { Success = false, ErrorMessage = "No budgets found for this company. Create a budget first." };
                budgetId = budgets[0].id;
            }

            var report = await svc.GetBudgetVsActualAsync(companyId, budgetId, fromDate, toDate, cashBasis);

            var sb = new StringBuilder();
            sb.AppendLine($"# Budget vs Actual — {report.BudgetName}");
            sb.AppendLine($"{(cashBasis ? "Cash Basis" : "Accrual Basis")} — {fromDate:MMMM d, yyyy} to {toDate:MMMM d, yyyy}");
            sb.AppendLine();

            sb.AppendLine("## Revenue");
            foreach (var sect in report.RevenueSections)
            {
                sb.AppendLine($"### {sect.Title}");
                foreach (var line in sect.Lines)
                    sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: Budget {line.BudgetedAmount:C} | Actual {line.ActualAmount:C} | Variance {line.Variance:C} ({line.VariancePercent:N1}%)");
                sb.AppendLine($"  **Subtotal: Budget {sect.TotalBudgeted:C} | Actual {sect.TotalActual:C}**");
            }
            sb.AppendLine($"**Total Revenue: Budget {report.TotalBudgetedRevenue:C} | Actual {report.TotalActualRevenue:C}**");
            sb.AppendLine();

            sb.AppendLine("## Expenses");
            foreach (var sect in report.ExpenseSections)
            {
                sb.AppendLine($"### {sect.Title}");
                foreach (var line in sect.Lines)
                    sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: Budget {line.BudgetedAmount:C} | Actual {line.ActualAmount:C} | Variance {line.Variance:C} ({line.VariancePercent:N1}%)");
                sb.AppendLine($"  **Subtotal: Budget {sect.TotalBudgeted:C} | Actual {sect.TotalActual:C}**");
            }
            sb.AppendLine($"**Total Expenses: Budget {report.TotalBudgetedExpenses:C} | Actual {report.TotalActualExpenses:C}**");
            sb.AppendLine();

            sb.AppendLine($"**Net Income: Budget {report.BudgetedNetIncome:C} | Actual {report.ActualNetIncome:C} | Variance {report.NetIncomeVariance:C} ({report.NetIncomeVariancePercent:N1}%)**");

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"Budget vs actual report for {report.BudgetName}",
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
