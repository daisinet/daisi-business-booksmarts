using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Retrieves the trial balance for a company as of a given date.
/// </summary>
public class TrialBalanceTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";
    private const string P_AS_OF_DATE = "as-of-date";
    private const string P_CASH_BASIS = "cash-basis";

    public override string Id => "booksmarts-trial-balance";
    public override string Name => "BookSmarts Trial Balance";

    public override string UseInstructions =>
        "Use this tool to get a trial balance for a company. " +
        "Lists all accounts with their debit and credit balances. " +
        "Keywords: trial balance, debit, credit, account balances, ledger.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID.", IsRequired = true },
        new() { Name = P_AS_OF_DATE, Description = "The date for the trial balance (yyyy-MM-dd). Defaults to today.", IsRequired = false },
        new() { Name = P_CASH_BASIS, Description = "true for cash basis, false for accrual. Defaults to false.", IsRequired = false }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;
        var dateStr = parameters.GetParameterValueOrDefault(P_AS_OF_DATE, null);
        var cashBasisStr = parameters.GetParameterValueOrDefault(P_CASH_BASIS, "false");

        var asOfDate = string.IsNullOrEmpty(dateStr) ? DateTime.Today : DateTime.Parse(dateStr);
        var cashBasis = string.Equals(cashBasisStr, "true", StringComparison.OrdinalIgnoreCase);

        return new ToolExecutionContext
        {
            ExecutionMessage = $"Generating trial balance as of {asOfDate:yyyy-MM-dd}",
            ExecutionTask = Execute(toolContext, companyId, asOfDate, cashBasis)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId, DateTime asOfDate, bool cashBasis)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<AccountingService>();
            var lines = await svc.GetTrialBalanceAsync(companyId, asOfDate, cashBasis);

            var sb = new StringBuilder();
            sb.AppendLine($"# Trial Balance — {(cashBasis ? "Cash Basis" : "Accrual Basis")}");
            sb.AppendLine($"As of {asOfDate:MMMM d, yyyy}");
            sb.AppendLine();
            sb.AppendLine("| Account # | Account Name | Debit | Credit |");
            sb.AppendLine("|-----------|-------------|------:|-------:|");

            decimal totalDebit = 0, totalCredit = 0;
            foreach (var line in lines)
            {
                sb.AppendLine($"| {line.AccountNumber} | {line.AccountName} | {(line.Debit != 0 ? line.Debit.ToString("N2") : "")} | {(line.Credit != 0 ? line.Credit.ToString("N2") : "")} |");
                totalDebit += line.Debit;
                totalCredit += line.Credit;
            }

            sb.AppendLine($"| | **Totals** | **{totalDebit:N2}** | **{totalCredit:N2}** |");
            sb.AppendLine();
            sb.AppendLine(totalDebit == totalCredit
                ? "Trial balance is in balance."
                : $"WARNING: Out of balance by {Math.Abs(totalDebit - totalCredit):C}.");

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"Trial balance as of {asOfDate:yyyy-MM-dd} ({lines.Count} accounts)",
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
