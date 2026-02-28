using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class CustomReportService(BookSmartsCosmo cosmo, AccountingService accounting, ChartOfAccountsService chartOfAccounts, EncryptionContext encryption)
{
    public async Task<CustomReport> CreateReportAsync(CustomReport report)
        => await cosmo.CreateCustomReportAsync(report);

    public async Task<CustomReport?> GetReportAsync(string id, string companyId)
        => await cosmo.GetCustomReportAsync(id, companyId);

    public async Task<List<CustomReport>> GetReportsAsync(string companyId)
        => await cosmo.GetCustomReportsAsync(companyId);

    public async Task<CustomReport> UpdateReportAsync(CustomReport report)
        => await cosmo.UpdateCustomReportAsync(report);

    public async Task DeleteReportAsync(string id, string companyId)
        => await cosmo.DeleteCustomReportAsync(id, companyId);

    /// <summary>
    /// Runs a custom report definition and returns the results.
    /// </summary>
    public async Task<CustomReportResult> RunReportAsync(
        CustomReport report, DateTime? asOfDate = null, DateTime? fromDate = null,
        DateTime? toDate = null, bool cashBasis = false)
    {
        var accounts = await chartOfAccounts.GetChartOfAccountsAsync(report.CompanyId, activeOnly: false);

        // Filter accounts based on report definition
        var filtered = accounts.AsEnumerable();

        if (report.AccountIds.Count > 0)
            filtered = filtered.Where(a => report.AccountIds.Contains(a.id));
        else
        {
            if (report.Categories.Count > 0)
                filtered = filtered.Where(a => report.Categories.Contains(a.Category));
            if (report.SubTypes.Count > 0)
                filtered = filtered.Where(a => report.SubTypes.Contains(a.SubType));
        }

        var targetAccounts = filtered.ToList();

        var result = new CustomReportResult
        {
            ReportName = report.Name,
            ReportType = report.ReportType,
            CashBasis = cashBasis
        };

        if (report.ReportType == CustomReportType.AccountBalances)
        {
            var date = asOfDate ?? DateTime.Today;
            result.AsOfDate = date;

            var trialBalance = await accounting.GetTrialBalanceAsync(report.CompanyId, date, cashBasis);
            var tbMap = trialBalance.ToDictionary(t => t.AccountId);

            var lines = targetAccounts.Select(a =>
            {
                tbMap.TryGetValue(a.id, out var tb);
                return new CustomReportLine
                {
                    AccountNumber = a.AccountNumber,
                    AccountName = a.Name,
                    Category = a.Category,
                    SubType = a.SubType,
                    Balance = tb?.Debit - tb?.Credit ?? 0m,
                    Debit = tb?.Debit ?? 0m,
                    Credit = tb?.Credit ?? 0m
                };
            }).ToList();

            if (!report.ShowZeroBalances)
                lines = lines.Where(l => l.Balance != 0 || l.Debit != 0 || l.Credit != 0).ToList();

            result.Groups = GroupLines(lines, report.Grouping);
            result.GrandTotal = lines.Sum(l => l.Balance);
        }
        else // IncomeExpense
        {
            var from = fromDate ?? new DateTime(DateTime.Today.Year, 1, 1);
            var to = toDate ?? DateTime.Today;
            result.FromDate = from;
            result.ToDate = to;

            // Get trial balance at end of period minus beginning of period
            var tbEnd = await accounting.GetTrialBalanceAsync(report.CompanyId, to, cashBasis);
            var tbStart = await accounting.GetTrialBalanceAsync(report.CompanyId, from.AddDays(-1), cashBasis);

            var endMap = tbEnd.ToDictionary(t => t.AccountId);
            var startMap = tbStart.ToDictionary(t => t.AccountId);

            var lines = targetAccounts.Select(a =>
            {
                endMap.TryGetValue(a.id, out var end);
                startMap.TryGetValue(a.id, out var start);

                decimal endBalance = (end?.Debit ?? 0m) - (end?.Credit ?? 0m);
                decimal startBalance = (start?.Debit ?? 0m) - (start?.Credit ?? 0m);
                decimal periodBalance = endBalance - startBalance;

                return new CustomReportLine
                {
                    AccountNumber = a.AccountNumber,
                    AccountName = a.Name,
                    Category = a.Category,
                    SubType = a.SubType,
                    Balance = periodBalance,
                    Debit = end?.Debit ?? 0m,
                    Credit = end?.Credit ?? 0m
                };
            }).ToList();

            if (!report.ShowZeroBalances)
                lines = lines.Where(l => l.Balance != 0).ToList();

            result.Groups = GroupLines(lines, report.Grouping);
            result.GrandTotal = lines.Sum(l => l.Balance);
        }

        return result;
    }

    private static List<CustomReportGroup> GroupLines(List<CustomReportLine> lines, CustomReportGrouping grouping)
    {
        return grouping switch
        {
            CustomReportGrouping.Category => lines
                .GroupBy(l => l.Category)
                .OrderBy(g => g.Key)
                .Select(g => new CustomReportGroup
                {
                    Title = g.Key.ToString(),
                    Lines = g.OrderBy(l => l.AccountNumber).ToList()
                }).ToList(),

            CustomReportGrouping.SubType => lines
                .GroupBy(l => l.SubType)
                .OrderBy(g => g.Key)
                .Select(g => new CustomReportGroup
                {
                    Title = g.Key.ToString(),
                    Lines = g.OrderBy(l => l.AccountNumber).ToList()
                }).ToList(),

            _ => new List<CustomReportGroup>
            {
                new()
                {
                    Title = "All Accounts",
                    Lines = lines.OrderBy(l => l.AccountNumber).ToList()
                }
            }
        };
    }
}
