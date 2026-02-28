using BookSmarts.Core.Models;
using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace BookSmarts.Tools;

/// <summary>
/// Creates and optionally posts a journal entry for a company.
/// </summary>
public class CreateJournalEntryTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";
    private const string P_DATE = "date";
    private const string P_DESCRIPTION = "description";
    private const string P_LINES = "lines";
    private const string P_AUTO_POST = "auto-post";

    public override string Id => "booksmarts-create-journal-entry";
    public override string Name => "BookSmarts Create Journal Entry";

    public override string UseInstructions =>
        "Use this tool to create a journal entry (double-entry bookkeeping transaction). " +
        "Provide the company ID, date, description, and lines as a JSON array. " +
        "Each line must have account-number, debit (or 0), and credit (or 0). " +
        "Total debits must equal total credits. " +
        "Lines format: [{\"account-number\":\"1000\",\"debit\":100,\"credit\":0,\"description\":\"Cash received\"}, " +
        "{\"account-number\":\"4000\",\"debit\":0,\"credit\":100,\"description\":\"Revenue earned\"}]. " +
        "Keywords: journal entry, record transaction, debit, credit, post entry, accounting entry.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID.", IsRequired = true },
        new() { Name = P_DATE, Description = "Entry date (yyyy-MM-dd). Defaults to today.", IsRequired = false },
        new() { Name = P_DESCRIPTION, Description = "Description of the journal entry.", IsRequired = true },
        new() { Name = P_LINES, Description = "JSON array of lines. Each line: {\"account-number\":\"...\", \"debit\":0, \"credit\":0, \"description\":\"...\"}.", IsRequired = true },
        new() { Name = P_AUTO_POST, Description = "true to auto-post after creation. Defaults to false (leaves as Draft).", IsRequired = false }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;
        var dateStr = parameters.GetParameterValueOrDefault(P_DATE, null);
        var description = parameters.GetParameter(P_DESCRIPTION)!.Value;
        var linesJson = parameters.GetParameter(P_LINES)!.Value;
        var autoPostStr = parameters.GetParameterValueOrDefault(P_AUTO_POST, "false");

        var date = string.IsNullOrEmpty(dateStr) ? DateTime.Today : DateTime.Parse(dateStr);
        var autoPost = string.Equals(autoPostStr, "true", StringComparison.OrdinalIgnoreCase);

        return new ToolExecutionContext
        {
            ExecutionMessage = $"Creating journal entry: {description}",
            ExecutionTask = Execute(toolContext, companyId, date, description, linesJson, autoPost)
        };
    }

    private static async Task<ToolResult> Execute(
        IToolContext ctx, string companyId, DateTime date, string description, string linesJson, bool autoPost)
    {
        try
        {
            var coaSvc = ctx.Services.GetRequiredService<ChartOfAccountsService>();
            var acctSvc = ctx.Services.GetRequiredService<AccountingService>();

            // Parse lines from JSON
            var lineInputs = JsonSerializer.Deserialize<List<LineInput>>(linesJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
            });

            if (lineInputs == null || lineInputs.Count < 2)
                return new ToolResult { Success = false, ErrorMessage = "At least two journal lines are required." };

            // Look up accounts by number
            var accounts = await coaSvc.GetChartOfAccountsAsync(companyId, activeOnly: true);
            var accountMap = accounts.ToDictionary(a => a.AccountNumber);

            var journalLines = new List<JournalLine>();
            foreach (var input in lineInputs)
            {
                if (!accountMap.TryGetValue(input.AccountNumber, out var account))
                    return new ToolResult { Success = false, ErrorMessage = $"Account number '{input.AccountNumber}' not found in chart of accounts." };

                journalLines.Add(new JournalLine
                {
                    AccountId = account.id,
                    AccountNumber = account.AccountNumber,
                    AccountName = account.Name,
                    Debit = input.Debit,
                    Credit = input.Credit,
                    Description = input.Description
                });
            }

            var entry = new JournalEntry
            {
                CompanyId = companyId,
                EntryDate = date,
                Description = description,
                Lines = journalLines
            };

            var created = await acctSvc.CreateJournalEntryAsync(entry);

            if (autoPost)
                created = await acctSvc.PostJournalEntryAsync(created.id, companyId);

            var sb = new StringBuilder();
            sb.AppendLine($"# Journal Entry {created.EntryNumber}");
            sb.AppendLine($"**Status:** {created.Status}");
            sb.AppendLine($"**Date:** {created.EntryDate:yyyy-MM-dd}");
            sb.AppendLine($"**Description:** {created.Description}");
            sb.AppendLine();
            sb.AppendLine("| # | Account | Debit | Credit | Description |");
            sb.AppendLine("|---|---------|------:|-------:|-------------|");

            foreach (var line in created.Lines)
                sb.AppendLine($"| {line.LineNumber} | {line.AccountNumber} {line.AccountName} | {(line.Debit != 0 ? line.Debit.ToString("N2") : "")} | {(line.Credit != 0 ? line.Credit.ToString("N2") : "")} | {line.Description} |");

            sb.AppendLine();
            sb.AppendLine($"**Totals — Debit: {created.TotalDebit:N2}, Credit: {created.TotalCredit:N2}**");

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"Journal entry {created.EntryNumber} created ({created.Status})",
                OutputFormat = InferenceOutputFormats.Markdown,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private class LineInput
    {
        public string AccountNumber { get; set; } = "";
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? Description { get; set; }
    }
}
