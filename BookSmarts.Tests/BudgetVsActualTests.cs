using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class BudgetVsActualTests
{
    private static List<FiscalPeriod> MakePeriods(DateTime yearStart)
    {
        var periods = new List<FiscalPeriod>();
        for (int i = 0; i < 12; i++)
        {
            var s = yearStart.AddMonths(i);
            periods.Add(new FiscalPeriod
            {
                PeriodId = $"p-{i + 1}",
                PeriodNumber = i + 1,
                Name = s.ToString("MMMM yyyy"),
                StartDate = s,
                EndDate = s.AddMonths(1).AddDays(-1)
            });
        }
        return periods;
    }

    [Fact]
    public void BuildBudgetVsActual_MatchesAccountsBetweenBudgetAndActuals()
    {
        var periods = MakePeriods(new DateTime(2026, 1, 1));
        var budget = new Budget
        {
            Name = "Test Budget",
            LineItems = new()
            {
                new BudgetLineItem { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Sales Revenue", PeriodId = "p-1", PeriodNumber = 1, Amount = 5000 },
                new BudgetLineItem { AccountId = "exp-1", AccountNumber = "5000", AccountName = "COGS", PeriodId = "p-1", PeriodNumber = 1, Amount = 2000 }
            }
        };

        var actuals = new IncomeStatementReport
        {
            RevenueSections = new()
            {
                new FinancialStatementSection
                {
                    Title = "Sales Revenue",
                    Lines = new() { new FinancialStatementLine { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Sales Revenue", Balance = 6000 } }
                }
            },
            ExpenseSections = new()
            {
                new FinancialStatementSection
                {
                    Title = "Cost of Goods Sold",
                    Lines = new() { new FinancialStatementLine { AccountId = "exp-1", AccountNumber = "5000", AccountName = "COGS", Balance = 2500 } }
                }
            },
            TotalRevenue = 6000,
            TotalExpenses = 2500
        };

        var report = BudgetService.BuildBudgetVsActual(budget, actuals, periods,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.Equal(6000m, report.TotalActualRevenue);
        Assert.Equal(5000m, report.TotalBudgetedRevenue);
        Assert.Equal(2500m, report.TotalActualExpenses);
        Assert.Equal(2000m, report.TotalBudgetedExpenses);
    }

    [Fact]
    public void BuildBudgetVsActual_FiltersToDateRange()
    {
        var periods = MakePeriods(new DateTime(2026, 1, 1));
        var budget = new Budget
        {
            Name = "Test",
            LineItems = new()
            {
                new BudgetLineItem { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue", PeriodId = "p-1", PeriodNumber = 1, Amount = 1000 },
                new BudgetLineItem { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue", PeriodId = "p-2", PeriodNumber = 2, Amount = 2000 },
                new BudgetLineItem { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue", PeriodId = "p-3", PeriodNumber = 3, Amount = 3000 }
            }
        };

        var actuals = new IncomeStatementReport
        {
            RevenueSections = new()
            {
                new FinancialStatementSection
                {
                    Title = "Revenue",
                    Lines = new() { new FinancialStatementLine { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue", Balance = 4000 } }
                }
            },
            ExpenseSections = new(),
            TotalRevenue = 4000,
            TotalExpenses = 0
        };

        // Only January (p-1) overlaps
        var report = BudgetService.BuildBudgetVsActual(budget, actuals, periods,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.Equal(1000m, report.TotalBudgetedRevenue);
    }

    [Fact]
    public void BuildBudgetVsActual_EmptyBudget_ReturnsActualsWithZeroBudget()
    {
        var periods = MakePeriods(new DateTime(2026, 1, 1));
        var budget = new Budget { Name = "Empty" };

        var actuals = new IncomeStatementReport
        {
            RevenueSections = new()
            {
                new FinancialStatementSection
                {
                    Title = "Revenue",
                    Lines = new() { new FinancialStatementLine { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue", Balance = 1000 } }
                }
            },
            ExpenseSections = new(),
            TotalRevenue = 1000,
            TotalExpenses = 0
        };

        var report = BudgetService.BuildBudgetVsActual(budget, actuals, periods,
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(0m, report.TotalBudgetedRevenue);
        Assert.Equal(1000m, report.TotalActualRevenue);
    }

    [Fact]
    public void BuildBudgetVsActual_EmptyActuals_ReturnsBudgetWithZeroActuals()
    {
        var periods = MakePeriods(new DateTime(2026, 1, 1));
        var budget = new Budget
        {
            Name = "Budget",
            LineItems = new()
            {
                new BudgetLineItem { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue", PeriodId = "p-1", PeriodNumber = 1, Amount = 5000 }
            }
        };

        var actuals = new IncomeStatementReport
        {
            RevenueSections = new(),
            ExpenseSections = new(),
            TotalRevenue = 0,
            TotalExpenses = 0
        };

        var report = BudgetService.BuildBudgetVsActual(budget, actuals, periods,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.Equal(5000m, report.TotalBudgetedRevenue);
        Assert.Equal(0m, report.TotalActualRevenue);
    }

    [Fact]
    public void BuildBudgetVsActual_GroupsBySection()
    {
        var periods = MakePeriods(new DateTime(2026, 1, 1));
        var budget = new Budget
        {
            Name = "Test",
            LineItems = new()
            {
                new BudgetLineItem { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Sales", PeriodId = "p-1", Amount = 3000 },
                new BudgetLineItem { AccountId = "rev-2", AccountNumber = "4100", AccountName = "Service", PeriodId = "p-1", Amount = 2000 }
            }
        };

        var actuals = new IncomeStatementReport
        {
            RevenueSections = new()
            {
                new FinancialStatementSection
                {
                    Title = "Sales Revenue",
                    Lines = new() { new FinancialStatementLine { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Sales", Balance = 3500 } }
                },
                new FinancialStatementSection
                {
                    Title = "Service Revenue",
                    Lines = new() { new FinancialStatementLine { AccountId = "rev-2", AccountNumber = "4100", AccountName = "Service", Balance = 2200 } }
                }
            },
            ExpenseSections = new(),
            TotalRevenue = 5700,
            TotalExpenses = 0
        };

        var report = BudgetService.BuildBudgetVsActual(budget, actuals, periods,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.Equal(2, report.RevenueSections.Count);
        Assert.Empty(report.ExpenseSections);
    }

    [Fact]
    public void BuildBudgetVsActual_VarianceCalcs_Correct()
    {
        var periods = MakePeriods(new DateTime(2026, 1, 1));
        var budget = new Budget
        {
            Name = "Test",
            LineItems = new()
            {
                new BudgetLineItem { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue", PeriodId = "p-1", Amount = 4000 },
                new BudgetLineItem { AccountId = "exp-1", AccountNumber = "5000", AccountName = "Expense", PeriodId = "p-1", Amount = 2000 }
            }
        };

        var actuals = new IncomeStatementReport
        {
            RevenueSections = new()
            {
                new FinancialStatementSection
                {
                    Title = "Revenue",
                    Lines = new() { new FinancialStatementLine { AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue", Balance = 5000 } }
                }
            },
            ExpenseSections = new()
            {
                new FinancialStatementSection
                {
                    Title = "Expense",
                    Lines = new() { new FinancialStatementLine { AccountId = "exp-1", AccountNumber = "5000", AccountName = "Expense", Balance = 1500 } }
                }
            },
            TotalRevenue = 5000,
            TotalExpenses = 1500
        };

        var report = BudgetService.BuildBudgetVsActual(budget, actuals, periods,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        // Net income: Budgeted = 4000 - 2000 = 2000, Actual = 5000 - 1500 = 3500
        Assert.Equal(2000m, report.BudgetedNetIncome);
        Assert.Equal(3500m, report.ActualNetIncome);
        Assert.Equal(1500m, report.NetIncomeVariance); // 3500 - 2000
    }

    // ── BvA model properties ──

    [Fact]
    public void BudgetVsActualLine_Variance_IsActualMinusBudget()
    {
        var line = new BudgetVsActualLine
        {
            BudgetedAmount = 1000,
            ActualAmount = 1200
        };

        Assert.Equal(200m, line.Variance);
    }

    [Fact]
    public void BudgetVsActualLine_VariancePercent_CalculatesCorrectly()
    {
        var line = new BudgetVsActualLine
        {
            BudgetedAmount = 1000,
            ActualAmount = 1250
        };

        Assert.Equal(25m, line.VariancePercent);
    }

    [Fact]
    public void BudgetVsActualLine_ZeroBudget_VariancePercentIsZero()
    {
        var line = new BudgetVsActualLine
        {
            BudgetedAmount = 0,
            ActualAmount = 500
        };

        Assert.Equal(0m, line.VariancePercent);
    }

    [Fact]
    public void BudgetVsActualSection_Totals_SumLines()
    {
        var section = new BudgetVsActualSection
        {
            Title = "Test",
            Lines = new()
            {
                new BudgetVsActualLine { BudgetedAmount = 100, ActualAmount = 120 },
                new BudgetVsActualLine { BudgetedAmount = 200, ActualAmount = 180 }
            }
        };

        Assert.Equal(300m, section.TotalBudgeted);
        Assert.Equal(300m, section.TotalActual);
        Assert.Equal(0m, section.TotalVariance); // (120-100) + (180-200) = 20 + -20 = 0
    }

    [Fact]
    public void BudgetVsActualReport_NetIncomeVariancePercent_CalculatesCorrectly()
    {
        var report = new BudgetVsActualReport
        {
            TotalBudgetedRevenue = 10000,
            TotalActualRevenue = 12000,
            TotalBudgetedExpenses = 5000,
            TotalActualExpenses = 4000
        };

        // Budgeted NI: 10000 - 5000 = 5000. Actual NI: 12000 - 4000 = 8000. Variance: 3000. Pct: 60%
        Assert.Equal(5000m, report.BudgetedNetIncome);
        Assert.Equal(8000m, report.ActualNetIncome);
        Assert.Equal(3000m, report.NetIncomeVariance);
        Assert.Equal(60m, report.NetIncomeVariancePercent);
    }
}
