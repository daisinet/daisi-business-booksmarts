using BookSmarts.Services;
using Daisi.Protos.V1;
using Daisi.SDK.Interfaces.Tools;
using Daisi.SDK.Models.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace BookSmarts.Tools;

/// <summary>
/// Lists outstanding (open) bills for a company.
/// </summary>
public class OutstandingBillsTool : DaisiToolBase
{
    private const string P_COMPANY_ID = "company-id";

    public override string Id => "booksmarts-outstanding-bills";
    public override string Name => "BookSmarts Outstanding Bills";

    public override string UseInstructions =>
        "Use this tool to get a list of outstanding (unpaid) bills for a company. " +
        "Shows bill numbers, vendors, amounts, due dates, and balances due. " +
        "Keywords: outstanding bills, unpaid bills, open bills, accounts payable, AP, what we owe, payables.";

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
            ExecutionMessage = "Retrieving outstanding bills",
            ExecutionTask = Execute(toolContext, companyId)
        };
    }

    private static async Task<ToolResult> Execute(IToolContext ctx, string companyId)
    {
        try
        {
            var svc = ctx.Services.GetRequiredService<BillService>();
            var bills = await svc.GetOpenBillsAsync(companyId);

            var sb = new StringBuilder();
            sb.AppendLine("# Outstanding Bills");
            sb.AppendLine();

            if (bills.Count == 0)
            {
                sb.AppendLine("No outstanding bills.");
            }
            else
            {
                sb.AppendLine("| Bill # | Vendor | Date | Due Date | Total | Paid | Balance Due |");
                sb.AppendLine("|--------|--------|------|----------|------:|-----:|------------:|");

                foreach (var bill in bills.OrderBy(b => b.DueDate))
                {
                    var overdue = bill.DueDate < DateTime.Today ? " **OVERDUE**" : "";
                    sb.AppendLine($"| {bill.BillNumber} | {bill.VendorName} | {bill.BillDate:yyyy-MM-dd} | {bill.DueDate:yyyy-MM-dd}{overdue} | {bill.Total:N2} | {bill.AmountPaid:N2} | {bill.BalanceDue:N2} |");
                }

                sb.AppendLine();
                sb.AppendLine($"**{bills.Count} bills outstanding, total balance due: {bills.Sum(b => b.BalanceDue):C}**");

                var overdueCount = bills.Count(b => b.DueDate < DateTime.Today);
                if (overdueCount > 0)
                    sb.AppendLine($"**{overdueCount} bills are overdue.**");
            }

            return new ToolResult
            {
                Output = sb.ToString(),
                OutputMessage = $"{bills.Count} outstanding bills",
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
