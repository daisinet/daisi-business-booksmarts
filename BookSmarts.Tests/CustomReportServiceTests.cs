using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;
using BookSmarts.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BookSmarts.Tests;

public class CustomReportServiceTests
{
    private static (CustomReportService service, Mock<BookSmartsCosmo> cosmo) CreateSut()
    {
        var cosmo = new Mock<BookSmartsCosmo>(Mock.Of<IConfiguration>(), "Cosmo:ConnectionString");
        var encContext = new EncryptionContext();
        var auditService = new AuditService(cosmo.Object);
        var accounting = new AccountingService(cosmo.Object, encContext, auditService);
        var coa = new ChartOfAccountsService(cosmo.Object, encContext);
        var service = new CustomReportService(cosmo.Object, accounting, coa, encContext);
        return (service, cosmo);
    }

    [Fact]
    public async Task CreateReportAsync_CreatesAndReturns()
    {
        var (service, cosmo) = CreateSut();
        CustomReport? captured = null;
        cosmo.Setup(c => c.CreateCustomReportAsync(It.IsAny<CustomReport>()))
            .Callback<CustomReport>(r => captured = r)
            .ReturnsAsync((CustomReport r) => { r.id = "rpt-1"; return r; });

        var report = new CustomReport
        {
            CompanyId = "co-1",
            Name = "Revenue Report",
            ReportType = CustomReportType.IncomeExpense,
            Categories = new List<AccountCategory> { AccountCategory.Revenue }
        };

        var result = await service.CreateReportAsync(report);

        Assert.NotNull(captured);
        Assert.Equal("Revenue Report", result.Name);
        Assert.Equal("rpt-1", result.id);
    }

    [Fact]
    public async Task RunReport_AccountBalances_ReturnsGroupedResults()
    {
        var (service, cosmo) = CreateSut();

        // Setup chart of accounts
        var accounts = new List<ChartOfAccountEntry>
        {
            new() { id = "a1", CompanyId = "co-1", AccountNumber = "1000", Name = "Cash", Category = AccountCategory.Asset, SubType = AccountSubType.Cash, IsActive = true },
            new() { id = "a2", CompanyId = "co-1", AccountNumber = "2000", Name = "AP", Category = AccountCategory.Liability, SubType = AccountSubType.AccountsPayable, IsActive = true },
            new() { id = "a3", CompanyId = "co-1", AccountNumber = "4000", Name = "Revenue", Category = AccountCategory.Revenue, SubType = AccountSubType.SalesRevenue, IsActive = true }
        };

        cosmo.Setup(c => c.GetChartOfAccountsAsync("co-1", false)).ReturnsAsync(accounts);

        // Setup journal entries for trial balance
        var entries = new List<JournalEntry>
        {
            new()
            {
                CompanyId = "co-1", Status = JournalEntryStatus.Posted,
                Lines = new List<JournalLine>
                {
                    new() { AccountId = "a1", Debit = 1000, Credit = 0 },
                    new() { AccountId = "a3", Debit = 0, Credit = 1000 }
                }
            }
        };

        cosmo.Setup(c => c.GetPostedJournalEntriesAsync("co-1", It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(entries);

        var report = new CustomReport
        {
            CompanyId = "co-1",
            Name = "Asset Balances",
            ReportType = CustomReportType.AccountBalances,
            Categories = new List<AccountCategory> { AccountCategory.Asset },
            Grouping = CustomReportGrouping.Category
        };

        var result = await service.RunReportAsync(report, asOfDate: DateTime.Today);

        Assert.Equal("Asset Balances", result.ReportName);
        Assert.Single(result.Groups);
        Assert.Equal("Asset", result.Groups[0].Title);
        Assert.Single(result.Groups[0].Lines);
        Assert.Equal("Cash", result.Groups[0].Lines[0].AccountName);
        Assert.Equal(1000m, result.Groups[0].Lines[0].Debit);
    }

    [Fact]
    public async Task RunReport_ZeroBalanceFiltering_Works()
    {
        var (service, cosmo) = CreateSut();

        var accounts = new List<ChartOfAccountEntry>
        {
            new() { id = "a1", CompanyId = "co-1", AccountNumber = "1000", Name = "Cash", Category = AccountCategory.Asset, SubType = AccountSubType.Cash, IsActive = true },
            new() { id = "a2", CompanyId = "co-1", AccountNumber = "1100", Name = "AR", Category = AccountCategory.Asset, SubType = AccountSubType.AccountsReceivable, IsActive = true }
        };

        cosmo.Setup(c => c.GetChartOfAccountsAsync("co-1", false)).ReturnsAsync(accounts);
        cosmo.Setup(c => c.GetPostedJournalEntriesAsync("co-1", It.IsAny<DateTime?>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<JournalEntry>());

        // Without zero balances
        var report = new CustomReport
        {
            CompanyId = "co-1",
            Name = "Test",
            ReportType = CustomReportType.AccountBalances,
            ShowZeroBalances = false
        };

        var result = await service.RunReportAsync(report);
        var totalLines = result.Groups.Sum(g => g.Lines.Count);
        Assert.Equal(0, totalLines);

        // With zero balances
        report.ShowZeroBalances = true;
        result = await service.RunReportAsync(report);
        totalLines = result.Groups.Sum(g => g.Lines.Count);
        Assert.Equal(2, totalLines);
    }
}
