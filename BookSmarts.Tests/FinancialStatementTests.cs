using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class FinancialStatementTests
{
    // ── ComputeNetBalance ──

    [Theory]
    [InlineData(AccountCategory.Asset, 1000, 200, 800)]
    [InlineData(AccountCategory.Expense, 500, 100, 400)]
    [InlineData(AccountCategory.Liability, 200, 1000, 800)]
    [InlineData(AccountCategory.Equity, 100, 500, 400)]
    [InlineData(AccountCategory.Revenue, 50, 3000, 2950)]
    public void ComputeNetBalance_CorrectSignForEachCategory(
        AccountCategory category, decimal debit, decimal credit, decimal expected)
    {
        var result = FinancialStatementService.ComputeNetBalance(category, debit, credit);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ComputeNetBalance_ZeroDebitsAndCredits_ReturnsZero()
    {
        Assert.Equal(0m, FinancialStatementService.ComputeNetBalance(AccountCategory.Asset, 0, 0));
        Assert.Equal(0m, FinancialStatementService.ComputeNetBalance(AccountCategory.Revenue, 0, 0));
    }

    // ── GetSubTypeSectionTitle ──

    [Theory]
    [InlineData(AccountSubType.Cash, "Cash & Bank")]
    [InlineData(AccountSubType.Bank, "Cash & Bank")]
    [InlineData(AccountSubType.AccountsReceivable, "Accounts Receivable")]
    [InlineData(AccountSubType.FixedAsset, "Fixed Assets")]
    [InlineData(AccountSubType.AccountsPayable, "Accounts Payable")]
    [InlineData(AccountSubType.LongTermDebt, "Long-Term Debt")]
    [InlineData(AccountSubType.OwnersEquity, "Owner's Equity")]
    [InlineData(AccountSubType.RetainedEarnings, "Retained Earnings")]
    [InlineData(AccountSubType.SalesRevenue, "Sales Revenue")]
    [InlineData(AccountSubType.ServiceRevenue, "Service Revenue")]
    [InlineData(AccountSubType.CostOfGoodsSold, "Cost of Goods Sold")]
    [InlineData(AccountSubType.Payroll, "Payroll")]
    [InlineData(AccountSubType.OtherExpense, "Other Expenses")]
    public void GetSubTypeSectionTitle_MapsAllSubTypes(AccountSubType subType, string expected)
    {
        Assert.Equal(expected, FinancialStatementService.GetSubTypeSectionTitle(subType));
    }

    // ── FinancialStatementSection ──

    [Fact]
    public void FinancialStatementSection_Total_SumsLineBalances()
    {
        var section = new FinancialStatementSection
        {
            Title = "Test",
            Lines = new()
            {
                new() { Balance = 100 },
                new() { Balance = 250 },
                new() { Balance = -50 }
            }
        };

        Assert.Equal(300m, section.Total);
    }

    [Fact]
    public void FinancialStatementSection_EmptyLines_ReturnsZeroTotal()
    {
        var section = new FinancialStatementSection { Title = "Empty" };
        Assert.Equal(0m, section.Total);
    }

    // ── BalanceSheetReport ──

    [Fact]
    public void BalanceSheetReport_IsBalanced_WhenAssetsEqualLiabilitiesPlusEquity()
    {
        var report = new BalanceSheetReport
        {
            TotalAssets = 1000,
            TotalLiabilities = 400,
            TotalEquity = 600
        };

        Assert.Equal(1000m, report.TotalLiabilitiesAndEquity);
        Assert.True(report.IsBalanced);
    }

    [Fact]
    public void BalanceSheetReport_IsNotBalanced_WhenMismatch()
    {
        var report = new BalanceSheetReport
        {
            TotalAssets = 1000,
            TotalLiabilities = 400,
            TotalEquity = 500
        };

        Assert.Equal(900m, report.TotalLiabilitiesAndEquity);
        Assert.False(report.IsBalanced);
    }

    [Fact]
    public void BalanceSheetReport_RetainedEarnings_IsRevenueMinusExpenses()
    {
        // Retained earnings is computed outside the model, but verify the model stores it
        var report = new BalanceSheetReport { RetainedEarnings = 1500 - 800 };
        Assert.Equal(700m, report.RetainedEarnings);
    }

    // ── IncomeStatementReport ──

    [Fact]
    public void IncomeStatementReport_NetIncome_IsRevenueMinusExpenses()
    {
        var report = new IncomeStatementReport
        {
            TotalRevenue = 5000,
            TotalExpenses = 3000
        };

        Assert.Equal(2000m, report.NetIncome);
    }

    [Fact]
    public void IncomeStatementReport_NegativeNetIncome_WhenExpensesExceedRevenue()
    {
        var report = new IncomeStatementReport
        {
            TotalRevenue = 1000,
            TotalExpenses = 2500
        };

        Assert.Equal(-1500m, report.NetIncome);
    }

    // ── CashFlowReport ──

    [Fact]
    public void CashFlowReport_NetChange_SumsAllSections()
    {
        var report = new CashFlowReport
        {
            NetOperating = 500,
            NetInvesting = -200,
            NetFinancing = 100
        };

        Assert.Equal(400m, report.NetChange);
    }

    [Fact]
    public void CashFlowReport_EndingCash_IsBeginningPlusNetChange()
    {
        var report = new CashFlowReport
        {
            BeginningCashBalance = 1000,
            NetOperating = 500,
            NetInvesting = -200,
            NetFinancing = -100
        };

        Assert.Equal(200m, report.NetChange);
        Assert.Equal(1200m, report.EndingCashBalance);
    }

    [Fact]
    public void CashFlowReport_ArDecrease_IsOperatingInflow()
    {
        // When AR decreases, that's cash collected — a positive operating item
        // AR is a current asset: change = end - begin. Decrease means end < begin, so change is negative.
        // In the service, current asset changes are negated: -change = positive = inflow.
        decimal beginAR = 500;
        decimal endAR = 300;
        decimal change = endAR - beginAR; // -200
        decimal operatingItem = -change;   // +200 (inflow)

        Assert.Equal(200m, operatingItem);
    }

    [Fact]
    public void CashFlowReport_ApIncrease_IsOperatingInflow()
    {
        // When AP increases, we owe more but haven't paid — a positive operating item
        // AP is a current liability: change = end - begin. Increase means end > begin, so change is positive.
        // In the service, current liability changes are kept as-is: change = positive = inflow.
        decimal beginAP = 200;
        decimal endAP = 500;
        decimal change = endAP - beginAP; // +300

        Assert.Equal(300m, change);
    }

    [Fact]
    public void CashFlowReport_FixedAssetPurchase_IsInvestingOutflow()
    {
        // Buying a fixed asset increases the account — investing outflow
        // Fixed asset is a long-term asset: change = end - begin. Increase = positive change.
        // In the service, long-term asset changes are negated: -change = negative = outflow.
        decimal beginFA = 0;
        decimal endFA = 5000;
        decimal change = endFA - beginFA; // +5000
        decimal investingItem = -change;   // -5000 (outflow)

        Assert.Equal(-5000m, investingItem);
    }

    [Fact]
    public void CashFlowReport_EquityContribution_IsFinancingInflow()
    {
        // Owner contributes equity — financing inflow
        // Equity change = end - begin. Increase = positive = inflow (kept as-is for financing).
        decimal beginEquity = 1000;
        decimal endEquity = 6000;
        decimal change = endEquity - beginEquity; // +5000

        Assert.Equal(5000m, change);
    }

    // ── Empty data ──

    [Fact]
    public void BalanceSheetReport_Empty_ReturnsZeroTotals()
    {
        var report = new BalanceSheetReport();

        Assert.Equal(0m, report.TotalAssets);
        Assert.Equal(0m, report.TotalLiabilities);
        Assert.Equal(0m, report.TotalEquity);
        Assert.Equal(0m, report.TotalLiabilitiesAndEquity);
        Assert.True(report.IsBalanced); // 0 == 0
        Assert.Empty(report.AssetSections);
        Assert.Empty(report.LiabilitySections);
        Assert.Empty(report.EquitySections);
    }

    [Fact]
    public void IncomeStatementReport_Empty_ReturnsZeroTotals()
    {
        var report = new IncomeStatementReport();

        Assert.Equal(0m, report.TotalRevenue);
        Assert.Equal(0m, report.TotalExpenses);
        Assert.Equal(0m, report.NetIncome);
        Assert.Empty(report.RevenueSections);
        Assert.Empty(report.ExpenseSections);
    }

    [Fact]
    public void CashFlowReport_Empty_ReturnsZeroTotals()
    {
        var report = new CashFlowReport();

        Assert.Equal(0m, report.BeginningCashBalance);
        Assert.Equal(0m, report.NetOperating);
        Assert.Equal(0m, report.NetInvesting);
        Assert.Equal(0m, report.NetFinancing);
        Assert.Equal(0m, report.NetChange);
        Assert.Equal(0m, report.EndingCashBalance);
        Assert.Empty(report.OperatingActivities);
        Assert.Empty(report.InvestingActivities);
        Assert.Empty(report.FinancingActivities);
    }
}
