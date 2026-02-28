using BookSmarts.Core.Enums;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class ChartOfAccountsTests
{
    [Theory]
    [InlineData(AccountCategory.Asset, NormalBalance.Debit)]
    [InlineData(AccountCategory.Expense, NormalBalance.Debit)]
    [InlineData(AccountCategory.Liability, NormalBalance.Credit)]
    [InlineData(AccountCategory.Equity, NormalBalance.Credit)]
    [InlineData(AccountCategory.Revenue, NormalBalance.Credit)]
    public void GetNormalBalance_ReturnsCorrectBalance(AccountCategory category, NormalBalance expected)
    {
        var result = ChartOfAccountsService.GetNormalBalance(category);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetDefaultChartOfAccounts_ReturnsAllCategories()
    {
        var accounts = ChartOfAccountsService.GetDefaultChartOfAccounts("co-test");

        Assert.NotEmpty(accounts);
        Assert.Contains(accounts, a => a.Category == AccountCategory.Asset);
        Assert.Contains(accounts, a => a.Category == AccountCategory.Liability);
        Assert.Contains(accounts, a => a.Category == AccountCategory.Equity);
        Assert.Contains(accounts, a => a.Category == AccountCategory.Revenue);
        Assert.Contains(accounts, a => a.Category == AccountCategory.Expense);
    }

    [Fact]
    public void GetDefaultChartOfAccounts_HasUniqueAccountNumbers()
    {
        var accounts = ChartOfAccountsService.GetDefaultChartOfAccounts("co-test");
        var numbers = accounts.Select(a => a.AccountNumber).ToList();
        Assert.Equal(numbers.Count, numbers.Distinct().Count());
    }

    [Fact]
    public void GetDefaultChartOfAccounts_AllHaveCompanyId()
    {
        var accounts = ChartOfAccountsService.GetDefaultChartOfAccounts("co-test");
        Assert.All(accounts, a => Assert.Equal("co-test", a.CompanyId));
    }

    [Fact]
    public void GetDefaultChartOfAccounts_AllHaveIds()
    {
        var accounts = ChartOfAccountsService.GetDefaultChartOfAccounts("co-test");
        Assert.All(accounts, a => Assert.False(string.IsNullOrEmpty(a.id)));
    }

    [Fact]
    public void GetDefaultChartOfAccounts_SystemAccountsExist()
    {
        var accounts = ChartOfAccountsService.GetDefaultChartOfAccounts("co-test");
        var system = accounts.Where(a => a.IsSystemAccount).ToList();

        Assert.True(system.Count >= 4, "Should have at least 4 system accounts (Cash, AR, AP, Equity)");
        Assert.Contains(system, a => a.SubType == AccountSubType.Cash);
        Assert.Contains(system, a => a.SubType == AccountSubType.AccountsReceivable);
        Assert.Contains(system, a => a.SubType == AccountSubType.AccountsPayable);
        Assert.Contains(system, a => a.SubType == AccountSubType.RetainedEarnings);
    }

    [Fact]
    public void GetDefaultChartOfAccounts_NormalBalancesCorrect()
    {
        var accounts = ChartOfAccountsService.GetDefaultChartOfAccounts("co-test");

        foreach (var account in accounts)
        {
            var expected = ChartOfAccountsService.GetNormalBalance(account.Category);
            Assert.Equal(expected, account.NormalBalance);
        }
    }

    [Fact]
    public void GetDefaultChartOfAccounts_AccountsAreSortableByNumber()
    {
        var accounts = ChartOfAccountsService.GetDefaultChartOfAccounts("co-test");
        var sorted = accounts.OrderBy(a => a.AccountNumber).ToList();

        // Assets (1xxx) before Liabilities (2xxx) before Equity (3xxx) before Revenue (4xxx) before Expense (5xxx+)
        var first = sorted.First();
        var last = sorted.Last();
        Assert.StartsWith("1", first.AccountNumber);
        Assert.True(string.Compare(last.AccountNumber, "5000") >= 0);
    }
}
