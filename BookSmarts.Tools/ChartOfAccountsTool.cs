using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Lists the chart of accounts for a company, optionally filtered by search term.
/// </summary>
public class ChartOfAccountsTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";
    private const string P_SEARCH = "search";
    private const string P_INCLUDE_INACTIVE = "include-inactive";

    public override string Id => "booksmarts-chart-of-accounts";
    public override string Name => "BookSmarts Chart of Accounts";

    public override string UseInstructions =>
        "Use this tool to list or search the chart of accounts for a company. " +
        "Returns account numbers, names, categories, and types. " +
        "Keywords: chart of accounts, COA, accounts, account list, account number, account name, ledger accounts.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID.", IsRequired = true },
        new() { Name = P_SEARCH, Description = "Optional search term to filter accounts by name or number.", IsRequired = false },
        new() { Name = P_INCLUDE_INACTIVE, Description = "true to include inactive accounts. Defaults to false.", IsRequired = false }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;
        var search = parameters.GetParameterValueOrDefault(P_SEARCH, null);
        var includeInactiveStr = parameters.GetParameterValueOrDefault(P_INCLUDE_INACTIVE, "false");
        var includeInactive = string.Equals(includeInactiveStr, "true", StringComparison.OrdinalIgnoreCase);

        return new ToolExecutionContext
        {
            ExecutionMessage = string.IsNullOrEmpty(search) ? "Listing chart of accounts" : $"Searching accounts for '{search}'",
            ExecutionTask = Execute(toolContext, companyId, search, includeInactive)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId, string? search, bool includeInactive)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<ChartOfAccountsService>();
            var accounts = await svc.GetChartOfAccountsAsync(companyId, activeOnly: !includeInactive);

            if (!string.IsNullOrEmpty(search))
            {
                accounts = accounts.Where(a =>
                    a.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    a.AccountNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (a.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            var sb = new StringBuilder();
            sb.AppendLine("# Chart of Accounts");
            sb.AppendLine();
            sb.AppendLine("| Account # | Name | Category | Sub-Type | Active |");
            sb.AppendLine("|-----------|------|----------|----------|--------|");

            foreach (var a in accounts.OrderBy(a => a.AccountNumber))
                sb.AppendLine($"| {a.AccountNumber} | {a.Name} | {a.Category} | {a.SubType} | {(a.IsActive ? "Yes" : "No")} |");

            sb.AppendLine();
            sb.AppendLine($"{accounts.Count} accounts found.");

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"Chart of accounts ({accounts.Count} accounts)",
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
