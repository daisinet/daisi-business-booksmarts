using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Generates AR or AP aging reports for a company.
/// </summary>
public class AgingReportTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";
    private const string P_REPORT_TYPE = "report-type";
    private const string P_AS_OF_DATE = "as-of-date";

    public override string Id => "booksmarts-aging-report";
    public override string Name => "BookSmarts Aging Report";

    public override string UseInstructions =>
        "Use this tool to get an accounts receivable (AR) or accounts payable (AP) aging report. " +
        "Shows amounts owed grouped by aging buckets (Current, 1-30, 31-60, 61-90, 90+ days). " +
        "Keywords: aging report, AR aging, AP aging, overdue, collections, past due, receivables aging, payables aging.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID.", IsRequired = true },
        new() { Name = P_REPORT_TYPE, Description = "ar for receivables aging, ap for payables aging.", IsRequired = true },
        new() { Name = P_AS_OF_DATE, Description = "Date for the aging report (yyyy-MM-dd). Defaults to today.", IsRequired = false }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;
        var reportType = parameters.GetParameter(P_REPORT_TYPE)!.Value.ToLowerInvariant();
        var dateStr = parameters.GetParameterValueOrDefault(P_AS_OF_DATE, null);
        var asOfDate = string.IsNullOrEmpty(dateStr) ? DateTime.Today : DateTime.Parse(dateStr);

        return new ToolExecutionContext
        {
            ExecutionMessage = $"Generating {reportType.ToUpper()} aging report",
            ExecutionTask = Execute(toolContext, companyId, reportType, asOfDate)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId, string reportType, DateTime asOfDate)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<AgingService>();
            var isAr = reportType == "ar";

            var (lines, summaries) = isAr
                ? await svc.GetArAgingReportAsync(companyId, asOfDate)
                : await svc.GetApAgingReportAsync(companyId, asOfDate);

            var title = isAr ? "Accounts Receivable" : "Accounts Payable";
            var contactLabel = isAr ? "Customer" : "Vendor";

            var sb = new StringBuilder();
            sb.AppendLine($"# {title} Aging Report");
            sb.AppendLine($"As of {asOfDate:MMMM d, yyyy}");
            sb.AppendLine();

            if (summaries.Count == 0)
            {
                sb.AppendLine($"No outstanding {(isAr ? "receivables" : "payables")}.");
            }
            else
            {
                sb.AppendLine($"## Summary by {contactLabel}");
                sb.AppendLine();
                sb.AppendLine($"| {contactLabel} | Current | 1-30 | 31-60 | 61-90 | 90+ | Total |");
                sb.AppendLine("|------------|--------:|-----:|------:|------:|----:|------:|");

                foreach (var s in summaries.OrderByDescending(s => s.Total))
                    sb.AppendLine($"| {s.ContactName} | {s.Current:N2} | {s.Days1To30:N2} | {s.Days31To60:N2} | {s.Days61To90:N2} | {s.Days90Plus:N2} | {s.Total:N2} |");

                var totals = new
                {
                    Current = summaries.Sum(s => s.Current),
                    D1 = summaries.Sum(s => s.Days1To30),
                    D31 = summaries.Sum(s => s.Days31To60),
                    D61 = summaries.Sum(s => s.Days61To90),
                    D90 = summaries.Sum(s => s.Days90Plus),
                    Total = summaries.Sum(s => s.Total)
                };
                sb.AppendLine($"| **Totals** | **{totals.Current:N2}** | **{totals.D1:N2}** | **{totals.D31:N2}** | **{totals.D61:N2}** | **{totals.D90:N2}** | **{totals.Total:N2}** |");

                sb.AppendLine();
                sb.AppendLine($"## Detail ({lines.Count} items)");
                sb.AppendLine();
                sb.AppendLine($"| Doc # | {contactLabel} | Date | Due Date | Days | Balance Due |");
                sb.AppendLine("|-------|------------|------|----------|-----:|------------:|");

                foreach (var l in lines.OrderBy(l => l.DueDate))
                    sb.AppendLine($"| {l.DocumentNumber} | {l.ContactName} | {l.DocumentDate:yyyy-MM-dd} | {l.DueDate:yyyy-MM-dd} | {l.DaysOutstanding} | {l.BalanceDue:N2} |");
            }

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"{title} aging ({summaries.Count} {contactLabel.ToLower()}s, {lines.Count} items)",
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
