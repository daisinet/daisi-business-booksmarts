using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Lists outstanding (open) invoices for a company.
/// </summary>
public class OutstandingInvoicesTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";

    public override string Id => "booksmarts-outstanding-invoices";
    public override string Name => "BookSmarts Outstanding Invoices";

    public override string UseInstructions =>
        "Use this tool to get a list of outstanding (unpaid) invoices for a company. " +
        "Shows invoice numbers, customers, amounts, due dates, and balances due. " +
        "Keywords: outstanding invoices, unpaid invoices, open invoices, accounts receivable, AR, who owes us, receivables.";

    public override ToolParameter[] Parameters =>
    [
        new() { Name = P_COMPANY_ID, Description = "The company ID.", IsRequired = true }
    ];

    public override ToolExecutionContext GetExecutionContext(
        IToolContext toolContext, CancellationToken cancellation, params ToolParameterBase[] parameters)
    {
        var companyId = parameters.GetParameter(P_COMPANY_ID)!.Value;

        return new ToolExecutionContext
        {
            ExecutionMessage = "Retrieving outstanding invoices",
            ExecutionTask = Execute(toolContext, companyId)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<InvoiceService>();
            var invoices = await svc.GetOpenInvoicesAsync(companyId);

            var sb = new StringBuilder();
            sb.AppendLine("# Outstanding Invoices");
            sb.AppendLine();

            if (invoices.Count == 0)
            {
                sb.AppendLine("No outstanding invoices.");
            }
            else
            {
                sb.AppendLine("| Invoice # | Customer | Date | Due Date | Total | Paid | Balance Due |");
                sb.AppendLine("|-----------|----------|------|----------|------:|-----:|------------:|");

                foreach (var inv in invoices.OrderBy(i => i.DueDate))
                {
                    var overdue = inv.DueDate < DateTime.Today ? " **OVERDUE**" : "";
                    sb.AppendLine($"| {inv.InvoiceNumber} | {inv.CustomerName} | {inv.InvoiceDate:yyyy-MM-dd} | {inv.DueDate:yyyy-MM-dd}{overdue} | {inv.Total:N2} | {inv.AmountPaid:N2} | {inv.BalanceDue:N2} |");
                }

                sb.AppendLine();
                sb.AppendLine($"**{invoices.Count} invoices outstanding, total balance due: {invoices.Sum(i => i.BalanceDue):C}**");

                var overdueCount = invoices.Count(i => i.DueDate < DateTime.Today);
                if (overdueCount > 0)
                    sb.AppendLine($"**{overdueCount} invoices are overdue.**");
            }

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"{invoices.Count} outstanding invoices",
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
