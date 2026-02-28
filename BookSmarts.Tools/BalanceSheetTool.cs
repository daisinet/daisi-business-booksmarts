using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Retrieves the balance sheet for a company as of a given date.
/// </summary>
public class BalanceSheetTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";
    private const string P_AS_OF_DATE = "as-of-date";
    private const string P_CASH_BASIS = "cash-basis";

    public override string Id => "booksmarts-balance-sheet";
    public override string Name => "BookSmarts Balance Sheet";

    public override string UseInstructions =>
        "Use this tool to get a balance sheet for a company. " +
        "Shows assets, liabilities, and equity as of a specific date. " +
        "Keywords: balance sheet, assets, liabilities, equity, financial position, net worth.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID to get the balance sheet for.", IsRequired = true },
        new() { Name = P_AS_OF_DATE, Description = "The date for the balance sheet (yyyy-MM-dd). Defaults to today.", IsRequired = false },
        new() { Name = P_CASH_BASIS, Description = "true for cash basis, false for accrual basis. Defaults to false.", IsRequired = false }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;
        var asOfDateStr = parameters.GetParameterValueOrDefault(P_AS_OF_DATE, null);
        var cashBasisStr = parameters.GetParameterValueOrDefault(P_CASH_BASIS, "false");

        var asOfDate = string.IsNullOrEmpty(asOfDateStr) ? DateTime.Today : DateTime.Parse(asOfDateStr);
        var cashBasis = string.Equals(cashBasisStr, "true", StringComparison.OrdinalIgnoreCase);

        return new ToolExecutionContext
        {
            ExecutionMessage = $"Generating balance sheet as of {asOfDate:yyyy-MM-dd}",
            ExecutionTask = Execute(toolContext, companyId, asOfDate, cashBasis)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId, DateTime asOfDate, bool cashBasis)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<FinancialStatementService>();
            var report = await svc.GetBalanceSheetAsync(companyId, asOfDate, cashBasis);

            var sb = new StringBuilder();
            sb.AppendLine($"# Balance Sheet — {(cashBasis ? "Cash Basis" : "Accrual Basis")}");
            sb.AppendLine($"As of {asOfDate:MMMM d, yyyy}");
            sb.AppendLine();

            sb.AppendLine("## Assets");
            foreach (var section in report.AssetSections)
            {
                sb.AppendLine($"### {section.Title}");
                foreach (var line in section.Lines)
                    sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: {line.Balance:C}");
                sb.AppendLine($"  **{section.Title} Total: {section.Total:C}**");
            }
            sb.AppendLine($"**Total Assets: {report.TotalAssets:C}**");
            sb.AppendLine();

            sb.AppendLine("## Liabilities");
            foreach (var section in report.LiabilitySections)
            {
                sb.AppendLine($"### {section.Title}");
                foreach (var line in section.Lines)
                    sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: {line.Balance:C}");
                sb.AppendLine($"  **{section.Title} Total: {section.Total:C}**");
            }
            sb.AppendLine($"**Total Liabilities: {report.TotalLiabilities:C}**");
            sb.AppendLine();

            sb.AppendLine("## Equity");
            foreach (var section in report.EquitySections)
            {
                sb.AppendLine($"### {section.Title}");
                foreach (var line in section.Lines)
                    sb.AppendLine($"  {line.AccountNumber} {line.AccountName}: {line.Balance:C}");
                sb.AppendLine($"  **{section.Title} Total: {section.Total:C}**");
            }
            sb.AppendLine($"**Total Equity: {report.TotalEquity:C}**");
            sb.AppendLine();
            sb.AppendLine($"**Total Liabilities & Equity: {report.TotalLiabilitiesAndEquity:C}**");
            sb.AppendLine(report.IsBalanced ? "Balance sheet is balanced." : "WARNING: Balance sheet is NOT balanced.");

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"Balance sheet as of {asOfDate:yyyy-MM-dd}",
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
