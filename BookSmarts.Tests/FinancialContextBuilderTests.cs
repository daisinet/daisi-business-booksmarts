using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class FinancialContextBuilderTests
{
    private readonly FinancialContextBuilder _builder = new();

    // ── Balance Sheet Context ──

    [Fact]
    public void BuildBalanceSheetContext_IncludesAsOfDate()
    {
        var report = new BalanceSheetReport
        {
            AsOfDate = new DateTime(2026, 1, 15),
            CashBasis = false,
            TotalAssets = 10000,
            TotalLiabilities = 4000,
            TotalEquity = 6000
        };

        var result = _builder.BuildBalanceSheetContext(report);

        Assert.Contains("2026-01-15", result);
        Assert.Contains("Accrual", result);
    }

    [Fact]
    public void BuildBalanceSheetContext_IncludesAllTotals()
    {
        var report = new BalanceSheetReport
        {
            AsOfDate = DateTime.Today,
            TotalAssets = 50000,
            TotalLiabilities = 20000,
            TotalEquity = 30000,
            RetainedEarnings = 15000
        };

        var result = _builder.BuildBalanceSheetContext(report);

        Assert.Contains("50,000.00", result);
        Assert.Contains("20,000.00", result);
        Assert.Contains("30,000.00", result);
        Assert.Contains("15,000.00", result);
    }

    [Fact]
    public void BuildBalanceSheetContext_ShowsBalancedStatus()
    {
        var report = new BalanceSheetReport
        {
            AsOfDate = DateTime.Today,
            TotalAssets = 10000,
            TotalLiabilities = 4000,
            TotalEquity = 6000
        };

        var result = _builder.BuildBalanceSheetContext(report);
        Assert.Contains("in balance", result);
    }

    [Fact]
    public void BuildBalanceSheetContext_ShowsOutOfBalanceStatus()
    {
        var report = new BalanceSheetReport
        {
            AsOfDate = DateTime.Today,
            TotalAssets = 10000,
            TotalLiabilities = 4000,
            TotalEquity = 5000
        };

        var result = _builder.BuildBalanceSheetContext(report);
        Assert.Contains("OUT OF BALANCE", result);
    }

    [Fact]
    public void BuildBalanceSheetContext_CashBasis_LabeledCorrectly()
    {
        var report = new BalanceSheetReport
        {
            AsOfDate = DateTime.Today,
            CashBasis = true
        };

        var result = _builder.BuildBalanceSheetContext(report);
        Assert.Contains("Cash", result);
    }

    [Fact]
    public void BuildBalanceSheetContext_IncludesSections()
    {
        var report = new BalanceSheetReport
        {
            AsOfDate = DateTime.Today,
            AssetSections = new()
            {
                new FinancialStatementSection
                {
                    Title = "Cash & Bank",
                    Lines = new()
                    {
                        new() { AccountNumber = "1010", AccountName = "Checking", Balance = 5000 }
                    }
                }
            }
        };

        var result = _builder.BuildBalanceSheetContext(report);
        Assert.Contains("Cash & Bank", result);
        Assert.Contains("1010", result);
        Assert.Contains("Checking", result);
        Assert.Contains("5,000.00", result);
    }

    // ── Income Statement Context ──

    [Fact]
    public void BuildIncomeStatementContext_IncludesDateRange()
    {
        var report = new IncomeStatementReport
        {
            FromDate = new DateTime(2026, 1, 1),
            ToDate = new DateTime(2026, 6, 30),
            CashBasis = false
        };

        var result = _builder.BuildIncomeStatementContext(report);

        Assert.Contains("2026-01-01", result);
        Assert.Contains("2026-06-30", result);
    }

    [Fact]
    public void BuildIncomeStatementContext_IncludesTotals()
    {
        var report = new IncomeStatementReport
        {
            FromDate = DateTime.Today.AddMonths(-1),
            ToDate = DateTime.Today,
            TotalRevenue = 25000,
            TotalExpenses = 18000
        };

        var result = _builder.BuildIncomeStatementContext(report);

        Assert.Contains("25,000.00", result);
        Assert.Contains("18,000.00", result);
        Assert.Contains("7,000.00", result); // NetIncome
    }

    // ── Cash Flow Context ──

    [Fact]
    public void BuildCashFlowContext_IncludesAllSections()
    {
        var report = new CashFlowReport
        {
            FromDate = new DateTime(2026, 1, 1),
            ToDate = new DateTime(2026, 6, 30),
            BeginningCashBalance = 10000,
            NetOperating = 5000,
            NetInvesting = -2000,
            NetFinancing = 1000,
            OperatingActivities = new()
            {
                new CashFlowItem { Label = "Net Income", Amount = 7000 }
            },
            InvestingActivities = new(),
            FinancingActivities = new()
        };

        var result = _builder.BuildCashFlowContext(report);

        Assert.Contains("Operating Activities", result);
        Assert.Contains("Investing Activities", result);
        Assert.Contains("Financing Activities", result);
        Assert.Contains("Net Income", result);
        Assert.Contains("10,000.00", result); // Beginning cash
        Assert.Contains("14,000.00", result); // Ending cash (10000 + 5000 - 2000 + 1000)
    }

    // ── Budget vs Actual Context ──

    [Fact]
    public void BuildBudgetVsActualContext_IncludesBudgetName()
    {
        var report = new BudgetVsActualReport
        {
            BudgetName = "Annual Budget 2026",
            FromDate = new DateTime(2026, 1, 1),
            ToDate = new DateTime(2026, 12, 31),
            TotalBudgetedRevenue = 100000,
            TotalActualRevenue = 95000,
            TotalBudgetedExpenses = 80000,
            TotalActualExpenses = 75000
        };

        var result = _builder.BuildBudgetVsActualContext(report);

        Assert.Contains("Annual Budget 2026", result);
        Assert.Contains("100,000.00", result);
        Assert.Contains("95,000.00", result);
    }

    [Fact]
    public void BuildBudgetVsActualContext_IncludesVariances()
    {
        var report = new BudgetVsActualReport
        {
            BudgetName = "Test",
            FromDate = DateTime.Today,
            ToDate = DateTime.Today,
            RevenueSections = new()
            {
                new BudgetVsActualSection
                {
                    Title = "Sales",
                    Lines = new()
                    {
                        new BudgetVsActualLine
                        {
                            AccountNumber = "4000",
                            AccountName = "Product Sales",
                            BudgetedAmount = 10000,
                            ActualAmount = 12000
                        }
                    }
                }
            }
        };

        var result = _builder.BuildBudgetVsActualContext(report);

        Assert.Contains("Product Sales", result);
        Assert.Contains("Budget", result);
        Assert.Contains("Actual", result);
        Assert.Contains("Variance", result);
    }

    // ── Aging Context ──

    [Fact]
    public void BuildAgingContext_AR_UsesCorrectLabels()
    {
        var summaries = new List<AgingReportSummary>
        {
            new()
            {
                ContactName = "Acme Corp",
                ContactId = "c1",
                Current = 1000,
                Days1To30 = 500,
                Days31To60 = 0,
                Days61To90 = 0,
                Days90Plus = 0
            }
        };

        var result = _builder.BuildAgingContext(summaries, new(), "ar");

        Assert.Contains("Accounts Receivable", result);
        Assert.Contains("Customer", result);
        Assert.Contains("Acme Corp", result);
        Assert.Contains("1,000.00", result);
    }

    [Fact]
    public void BuildAgingContext_AP_UsesCorrectLabels()
    {
        var summaries = new List<AgingReportSummary>
        {
            new()
            {
                ContactName = "Supplier Inc",
                ContactId = "v1",
                Current = 2000,
                Days1To30 = 0,
                Days31To60 = 300,
                Days61To90 = 0,
                Days90Plus = 0
            }
        };

        var result = _builder.BuildAgingContext(summaries, new(), "ap");

        Assert.Contains("Accounts Payable", result);
        Assert.Contains("Vendor", result);
        Assert.Contains("Supplier Inc", result);
    }

    [Fact]
    public void BuildAgingContext_IncludesGrandTotals()
    {
        var summaries = new List<AgingReportSummary>
        {
            new() { ContactName = "A", ContactId = "1", Current = 1000, Days1To30 = 200 },
            new() { ContactName = "B", ContactId = "2", Current = 500, Days1To30 = 300 }
        };

        var result = _builder.BuildAgingContext(summaries, new(), "ar");

        Assert.Contains("1,500.00", result); // Total Current
        Assert.Contains("500.00", result); // Total 1-30
        Assert.Contains("Totals", result);
    }

    // ── Projection Context ──

    [Fact]
    public void BuildProjectionContext_IncludesHistoricalData()
    {
        var statements = new List<IncomeStatementReport>
        {
            new()
            {
                FromDate = new DateTime(2025, 7, 1),
                ToDate = new DateTime(2025, 7, 31),
                TotalRevenue = 10000,
                TotalExpenses = 8000
            }
        };

        var bs = new BalanceSheetReport
        {
            AsOfDate = DateTime.Today,
            TotalAssets = 50000,
            TotalLiabilities = 20000,
            TotalEquity = 30000
        };

        var cf = new CashFlowReport
        {
            FromDate = new DateTime(2026, 1, 1),
            ToDate = DateTime.Today,
            NetOperating = 5000
        };

        var result = _builder.BuildProjectionContext(statements, bs, cf);

        Assert.Contains("Jul 2025", result);
        Assert.Contains("10,000.00", result);
        Assert.Contains("Current Balance Sheet", result);
        Assert.Contains("Current Cash Flow", result);
    }

    // ── Empty data ──

    [Fact]
    public void BuildBalanceSheetContext_EmptyReport_StillFormats()
    {
        var report = new BalanceSheetReport { AsOfDate = DateTime.Today };
        var result = _builder.BuildBalanceSheetContext(report);

        Assert.NotNull(result);
        Assert.Contains("Balance Sheet", result);
        Assert.Contains("0.00", result);
    }

    [Fact]
    public void BuildIncomeStatementContext_EmptyReport_StillFormats()
    {
        var report = new IncomeStatementReport
        {
            FromDate = DateTime.Today,
            ToDate = DateTime.Today
        };
        var result = _builder.BuildIncomeStatementContext(report);

        Assert.NotNull(result);
        Assert.Contains("Income Statement", result);
    }

    [Fact]
    public void BuildAgingContext_EmptySummaries_StillFormats()
    {
        var result = _builder.BuildAgingContext(new(), new(), "ar");

        Assert.NotNull(result);
        Assert.Contains("Accounts Receivable", result);
    }
}
