using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Retrieves the statement of cash flows for a company over a date range.
/// </summary>
public class CashFlowTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";
    private const string P_FROM_DATE = "from-date";
    private const string P_TO_DATE = "to-date";

    public override string Id => "booksmarts-cash-flow";
    public override string Name => "BookSmarts Cash Flow Statement";

    public override string UseInstructions =>
        "Use this tool to get a statement of cash flows for a company. " +
        "Shows operating, investing, and financing cash flows for a date range. " +
        "Keywords: cash flow, cash position, operating cash, investing, financing, liquidity.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID to get the cash flow statement for.", IsRequired = true },
        new() { Name = P_FROM_DATE, Description = "Start date for the period (yyyy-MM-dd). Defaults to start of current year.", IsRequired = false },
        new() { Name = P_TO_DATE, Description = "End date for the period (yyyy-MM-dd). Defaults to today.", IsRequired = false }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;
        var fromDateStr = parameters.GetParameterValueOrDefault(P_FROM_DATE, null);
        var toDateStr = parameters.GetParameterValueOrDefault(P_TO_DATE, null);

        var fromDate = string.IsNullOrEmpty(fromDateStr) ? new DateTime(DateTime.Today.Year, 1, 1) : DateTime.Parse(fromDateStr);
        var toDate = string.IsNullOrEmpty(toDateStr) ? DateTime.Today : DateTime.Parse(toDateStr);

        return new ToolExecutionContext
        {
            ExecutionMessage = $"Generating cash flow statement from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
            ExecutionTask = Execute(toolContext, companyId, fromDate, toDate)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId, DateTime fromDate, DateTime toDate)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<FinancialStatementService>();
            var report = await svc.GetCashFlowStatementAsync(companyId, fromDate, toDate);

            var sb = new StringBuilder();
            sb.AppendLine("# Statement of Cash Flows");
            sb.AppendLine($"Period: {fromDate:MMMM d, yyyy} to {toDate:MMMM d, yyyy}");
            sb.AppendLine();

            sb.AppendLine("## Operating Activities");
            foreach (var item in report.OperatingActivities)
                sb.AppendLine($"  {item.Label}: {item.Amount:C}");
            sb.AppendLine($"  **Net Cash from Operating Activities: {report.NetOperating:C}**");
            sb.AppendLine();

            sb.AppendLine("## Investing Activities");
            if (report.InvestingActivities.Count == 0)
                sb.AppendLine("  None");
            else
                foreach (var item in report.InvestingActivities)
                    sb.AppendLine($"  {item.Label}: {item.Amount:C}");
            sb.AppendLine($"  **Net Cash from Investing Activities: {report.NetInvesting:C}**");
            sb.AppendLine();

            sb.AppendLine("## Financing Activities");
            if (report.FinancingActivities.Count == 0)
                sb.AppendLine("  None");
            else
                foreach (var item in report.FinancingActivities)
                    sb.AppendLine($"  {item.Label}: {item.Amount:C}");
            sb.AppendLine($"  **Net Cash from Financing Activities: {report.NetFinancing:C}**");
            sb.AppendLine();

            sb.AppendLine($"**Net Change in Cash: {report.NetChange:C}**");
            sb.AppendLine($"Beginning Cash Balance: {report.BeginningCashBalance:C}");
            sb.AppendLine($"**Ending Cash Balance: {report.EndingCashBalance:C}**");

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"Cash flow statement from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
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
