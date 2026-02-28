using BookSmarts.Core.Enums;
using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Retrieves recent bank transactions for a company, with optional status filtering.
/// </summary>
public class BankTransactionsTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";
    private const string P_STATUS = "status";
    private const string P_FROM_DATE = "from-date";
    private const string P_TO_DATE = "to-date";

    public override string Id => "booksmarts-bank-transactions";
    public override string Name => "BookSmarts Bank Transactions";

    public override string UseInstructions =>
        "Use this tool to get recent bank transactions for a company. " +
        "Can filter by status (Uncategorized, Categorized, Excluded) and date range. " +
        "Keywords: bank transactions, bank activity, spending, deposits, uncategorized, bank imports.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID to get bank transactions for.", IsRequired = true },
        new() { Name = P_STATUS, Description = "Filter by status: Uncategorized, Categorized, or Excluded. Defaults to all.", IsRequired = false },
        new() { Name = P_FROM_DATE, Description = "Start date filter (yyyy-MM-dd). Defaults to 30 days ago.", IsRequired = false },
        new() { Name = P_TO_DATE, Description = "End date filter (yyyy-MM-dd). Defaults to today.", IsRequired = false }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;
        var statusStr = parameters.GetParameterValueOrDefault(P_STATUS, null);
        var fromDateStr = parameters.GetParameterValueOrDefault(P_FROM_DATE, null);
        var toDateStr = parameters.GetParameterValueOrDefault(P_TO_DATE, null);

        BankTransactionStatus? status = null;
        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<BankTransactionStatus>(statusStr, true, out var parsed))
            status = parsed;

        var fromDate = string.IsNullOrEmpty(fromDateStr) ? DateTime.Today.AddDays(-30) : DateTime.Parse(fromDateStr);
        var toDate = string.IsNullOrEmpty(toDateStr) ? DateTime.Today : DateTime.Parse(toDateStr);

        return new ToolExecutionContext
        {
            ExecutionMessage = $"Fetching bank transactions from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
            ExecutionTask = Execute(toolContext, companyId, status, fromDate, toDate)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId, BankTransactionStatus? status, DateTime fromDate, DateTime toDate)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<BankingService>();
            var transactions = await svc.GetTransactionsAsync(companyId, status, null, null, fromDate, toDate);

            if (transactions.Count == 0)
            {
                return new ToolResult
                {
                    Output = "No bank transactions found for the specified criteria.",
                    OutputMessage = "No transactions found",
                    OutputFormat = InferenceOutputFormats.Markdown,
                    Success = true
                };
            }

            var sb = new StringBuilder();
            sb.AppendLine("# Bank Transactions");
            sb.AppendLine($"Period: {fromDate:MMMM d, yyyy} to {toDate:MMMM d, yyyy}");
            if (status.HasValue)
                sb.AppendLine($"Filter: {status.Value}");
            sb.AppendLine($"Total: {transactions.Count} transactions");
            sb.AppendLine();

            sb.AppendLine("| Date | Description | Amount | Status |");
            sb.AppendLine("|------|-------------|--------|--------|");

            foreach (var txn in transactions.OrderByDescending(t => t.TransactionDate).Take(50))
            {
                var amountStr = txn.Amount >= 0 ? $"+{txn.Amount:N2}" : $"{txn.Amount:N2}";
                sb.AppendLine($"| {txn.TransactionDate:yyyy-MM-dd} | {txn.MerchantName ?? txn.Name} | {amountStr} | {txn.Status} |");
            }

            if (transactions.Count > 50)
                sb.AppendLine($"\n*Showing 50 of {transactions.Count} transactions.*");

            // Summary
            sb.AppendLine();
            var totalIn = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var totalOut = transactions.Where(t => t.Amount < 0).Sum(t => t.Amount);
            sb.AppendLine($"**Total Inflows: {totalIn:C}**");
            sb.AppendLine($"**Total Outflows: {totalOut:C}**");
            sb.AppendLine($"**Net: {(totalIn + totalOut):C}**");

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"{transactions.Count} bank transactions",
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
