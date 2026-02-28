using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class AgingService(BookSmartsCosmo cosmo, EncryptionContext encryption)
{
    /// <summary>
    /// Generates an AR aging report from open invoices, bucketed by days past due.
    /// </summary>
    public async Task<(List<AgingReportLine> Lines, List<AgingReportSummary> Summaries)> GetArAgingReportAsync(string companyId, DateTime asOfDate)
    {
        var invoices = await cosmo.GetOpenInvoicesAsync(companyId);
        DecryptAll(invoices);

        var lines = invoices.Select(inv => new AgingReportLine
        {
            DocumentId = inv.id,
            DocumentNumber = inv.InvoiceNumber,
            ContactName = inv.CustomerName,
            ContactId = inv.CustomerId,
            DocumentDate = inv.InvoiceDate,
            DueDate = inv.DueDate,
            Total = inv.Total,
            BalanceDue = inv.BalanceDue,
            DaysOutstanding = (int)(asOfDate - inv.DueDate).TotalDays,
            AgingBucket = GetAgingBucket((int)(asOfDate - inv.DueDate).TotalDays)
        }).ToList();

        var summaries = BuildSummaries(lines);
        return (lines, summaries);
    }

    /// <summary>
    /// Generates an AP aging report from open bills, bucketed by days past due.
    /// </summary>
    public async Task<(List<AgingReportLine> Lines, List<AgingReportSummary> Summaries)> GetApAgingReportAsync(string companyId, DateTime asOfDate)
    {
        var bills = await cosmo.GetOpenBillsAsync(companyId);
        DecryptAll(bills);

        var lines = bills.Select(bill => new AgingReportLine
        {
            DocumentId = bill.id,
            DocumentNumber = bill.BillNumber,
            ContactName = bill.VendorName,
            ContactId = bill.VendorId,
            DocumentDate = bill.BillDate,
            DueDate = bill.DueDate,
            Total = bill.Total,
            BalanceDue = bill.BalanceDue,
            DaysOutstanding = (int)(asOfDate - bill.DueDate).TotalDays,
            AgingBucket = GetAgingBucket((int)(asOfDate - bill.DueDate).TotalDays)
        }).ToList();

        var summaries = BuildSummaries(lines);
        return (lines, summaries);
    }

    public static string GetAgingBucket(int daysOutstanding)
    {
        return daysOutstanding switch
        {
            <= 0 => "Current",
            <= 30 => "1-30",
            <= 60 => "31-60",
            <= 90 => "61-90",
            _ => "90+"
        };
    }

    public static List<AgingReportSummary> BuildSummaries(List<AgingReportLine> lines)
    {
        return lines
            .GroupBy(l => new { l.ContactId, l.ContactName })
            .Select(g => new AgingReportSummary
            {
                ContactId = g.Key.ContactId,
                ContactName = g.Key.ContactName,
                Current = g.Where(l => l.AgingBucket == "Current").Sum(l => l.BalanceDue),
                Days1To30 = g.Where(l => l.AgingBucket == "1-30").Sum(l => l.BalanceDue),
                Days31To60 = g.Where(l => l.AgingBucket == "31-60").Sum(l => l.BalanceDue),
                Days61To90 = g.Where(l => l.AgingBucket == "61-90").Sum(l => l.BalanceDue),
                Days90Plus = g.Where(l => l.AgingBucket == "90+").Sum(l => l.BalanceDue),
            })
            .OrderBy(s => s.ContactName)
            .ToList();
    }

    // ── Encryption helpers ──

    private void Encrypt<T>(T model) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.EncryptFields(model, adk);
    }

    private T Decrypt<T>(T model) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.DecryptFields(model, adk);
        return model;
    }

    private List<T> DecryptAll<T>(List<T> models) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.DecryptAll(models, adk);
        return models;
    }
}
