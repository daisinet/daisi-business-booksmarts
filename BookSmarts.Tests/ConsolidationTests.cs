using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class ConsolidationTests
{
    // ── MergeBalanceSheets ──

    [Fact]
    public void MergeBalanceSheets_SumsAcrossCompanies()
    {
        var reports = new List<BalanceSheetReport>
        {
            new()
            {
                AsOfDate = DateTime.Today,
                AssetSections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Cash & Bank",
                        SubType = AccountSubType.Cash,
                        Lines = new() { new FinancialStatementLine { AccountId = "a-1", AccountName = "Cash", Balance = 1000 } }
                    }
                },
                LiabilitySections = new(),
                EquitySections = new(),
                TotalAssets = 1000
            },
            new()
            {
                AsOfDate = DateTime.Today,
                AssetSections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Cash & Bank",
                        SubType = AccountSubType.Cash,
                        Lines = new() { new FinancialStatementLine { AccountId = "a-2", AccountName = "Cash", Balance = 2000 } }
                    }
                },
                LiabilitySections = new(),
                EquitySections = new(),
                TotalAssets = 2000
            }
        };

        var merged = ConsolidationService.MergeBalanceSheets(reports);

        Assert.Single(merged.AssetSections);
        Assert.Equal(3000m, merged.TotalAssets);
    }

    [Fact]
    public void MergeBalanceSheets_EmptyInput_ReturnsEmpty()
    {
        var merged = ConsolidationService.MergeBalanceSheets(new List<BalanceSheetReport>());

        Assert.Equal(0m, merged.TotalAssets);
        Assert.Equal(0m, merged.TotalLiabilities);
        Assert.Equal(0m, merged.TotalEquity);
        Assert.Empty(merged.AssetSections);
    }

    [Fact]
    public void MergeBalanceSheets_SingleReport_ReturnsSameValues()
    {
        var reports = new List<BalanceSheetReport>
        {
            new()
            {
                AsOfDate = DateTime.Today,
                AssetSections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Cash & Bank",
                        SubType = AccountSubType.Cash,
                        Lines = new() { new FinancialStatementLine { AccountId = "a-1", AccountName = "Cash", Balance = 5000 } }
                    }
                },
                LiabilitySections = new(),
                EquitySections = new(),
                TotalAssets = 5000
            }
        };

        var merged = ConsolidationService.MergeBalanceSheets(reports);

        Assert.Equal(5000m, merged.TotalAssets);
        Assert.Single(merged.AssetSections);
    }

    [Fact]
    public void MergeBalanceSheets_ResultIsBalanced()
    {
        var reports = new List<BalanceSheetReport>
        {
            new()
            {
                AsOfDate = DateTime.Today,
                AssetSections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Cash & Bank",
                        SubType = AccountSubType.Cash,
                        Lines = new() { new FinancialStatementLine { AccountId = "a-1", AccountName = "Cash", Balance = 1000 } }
                    }
                },
                LiabilitySections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Accounts Payable",
                        SubType = AccountSubType.AccountsPayable,
                        Lines = new() { new FinancialStatementLine { AccountId = "l-1", AccountName = "AP", Balance = 400 } }
                    }
                },
                EquitySections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Owner's Equity",
                        SubType = AccountSubType.OwnersEquity,
                        Lines = new() { new FinancialStatementLine { AccountId = "e-1", AccountName = "Equity", Balance = 600 } }
                    }
                },
                TotalAssets = 1000,
                TotalLiabilities = 400,
                TotalEquity = 600
            }
        };

        var merged = ConsolidationService.MergeBalanceSheets(reports);
        Assert.True(merged.IsBalanced);
    }

    // ── MergeIncomeStatements ──

    [Fact]
    public void MergeIncomeStatements_SumsRevenueAndExpenses()
    {
        var reports = new List<IncomeStatementReport>
        {
            new()
            {
                FromDate = DateTime.Today.AddDays(-30),
                ToDate = DateTime.Today,
                RevenueSections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Sales Revenue",
                        Lines = new() { new FinancialStatementLine { AccountId = "r-1", AccountName = "Sales", Balance = 5000 } }
                    }
                },
                ExpenseSections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Payroll",
                        Lines = new() { new FinancialStatementLine { AccountId = "e-1", AccountName = "Payroll", Balance = 2000 } }
                    }
                },
                TotalRevenue = 5000,
                TotalExpenses = 2000
            },
            new()
            {
                FromDate = DateTime.Today.AddDays(-30),
                ToDate = DateTime.Today,
                RevenueSections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Sales Revenue",
                        Lines = new() { new FinancialStatementLine { AccountId = "r-2", AccountName = "Sales", Balance = 3000 } }
                    }
                },
                ExpenseSections = new()
                {
                    new FinancialStatementSection
                    {
                        Title = "Payroll",
                        Lines = new() { new FinancialStatementLine { AccountId = "e-2", AccountName = "Payroll", Balance = 1500 } }
                    }
                },
                TotalRevenue = 3000,
                TotalExpenses = 1500
            }
        };

        var merged = ConsolidationService.MergeIncomeStatements(reports);

        Assert.Equal(8000m, merged.TotalRevenue);
        Assert.Equal(3500m, merged.TotalExpenses);
        Assert.Equal(4500m, merged.NetIncome);
    }

    [Fact]
    public void MergeIncomeStatements_EmptyInput_ReturnsEmpty()
    {
        var merged = ConsolidationService.MergeIncomeStatements(new List<IncomeStatementReport>());

        Assert.Equal(0m, merged.TotalRevenue);
        Assert.Equal(0m, merged.TotalExpenses);
        Assert.Equal(0m, merged.NetIncome);
    }

    // ── BuildBalanceSheetEliminations ──

    [Fact]
    public void BuildBalanceSheetEliminations_CreatesOffsettingEntries()
    {
        var icTransactions = new List<InterCompanyTransaction>
        {
            new()
            {
                Amount = 1000,
                TransactionDate = DateTime.Today.AddDays(-10),
                Status = InterCompanyStatus.Posted,
                EliminateOnConsolidation = true
            },
            new()
            {
                Amount = 500,
                TransactionDate = DateTime.Today.AddDays(-5),
                Status = InterCompanyStatus.Posted,
                EliminateOnConsolidation = true
            }
        };

        var result = ConsolidationService.BuildBalanceSheetEliminations(icTransactions, DateTime.Today);

        Assert.NotNull(result);
        Assert.Equal(-1500m, result!.TotalAssets);
        Assert.Equal(-1500m, result.TotalLiabilities);
    }

    [Fact]
    public void BuildBalanceSheetEliminations_NoTransactions_ReturnsNull()
    {
        var result = ConsolidationService.BuildBalanceSheetEliminations(new List<InterCompanyTransaction>(), DateTime.Today);
        Assert.Null(result);
    }

    [Fact]
    public void BuildBalanceSheetEliminations_VoidedTransactions_Excluded()
    {
        var icTransactions = new List<InterCompanyTransaction>
        {
            new()
            {
                Amount = 1000,
                TransactionDate = DateTime.Today,
                Status = InterCompanyStatus.Voided,
                EliminateOnConsolidation = true
            }
        };

        var result = ConsolidationService.BuildBalanceSheetEliminations(icTransactions, DateTime.Today);
        Assert.Null(result);
    }

    [Fact]
    public void BuildBalanceSheetEliminations_FiltersAsOfDate()
    {
        var icTransactions = new List<InterCompanyTransaction>
        {
            new()
            {
                Amount = 1000,
                TransactionDate = new DateTime(2026, 1, 15),
                Status = InterCompanyStatus.Posted,
                EliminateOnConsolidation = true
            },
            new()
            {
                Amount = 2000,
                TransactionDate = new DateTime(2026, 3, 15),
                Status = InterCompanyStatus.Posted,
                EliminateOnConsolidation = true
            }
        };

        // Only the Jan transaction should be included
        var result = ConsolidationService.BuildBalanceSheetEliminations(icTransactions, new DateTime(2026, 1, 31));

        Assert.NotNull(result);
        Assert.Equal(-1000m, result!.TotalAssets);
    }

    // ── BuildIncomeStatementEliminations ──

    [Fact]
    public void BuildIncomeStatementEliminations_CreatesOffsettingEntries()
    {
        var icTransactions = new List<InterCompanyTransaction>
        {
            new()
            {
                Amount = 1000,
                TransactionDate = new DateTime(2026, 1, 15),
                Status = InterCompanyStatus.Posted,
                EliminateOnConsolidation = true
            }
        };

        var result = ConsolidationService.BuildIncomeStatementEliminations(
            icTransactions, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.NotNull(result);
        Assert.Equal(-1000m, result!.TotalRevenue);
        Assert.Equal(-1000m, result.TotalExpenses);
    }

    [Fact]
    public void BuildIncomeStatementEliminations_FiltersDateRange()
    {
        var icTransactions = new List<InterCompanyTransaction>
        {
            new()
            {
                Amount = 1000,
                TransactionDate = new DateTime(2026, 1, 15),
                Status = InterCompanyStatus.Posted,
                EliminateOnConsolidation = true
            },
            new()
            {
                Amount = 2000,
                TransactionDate = new DateTime(2026, 3, 15),
                Status = InterCompanyStatus.Posted,
                EliminateOnConsolidation = true
            }
        };

        // Only January transaction in range
        var result = ConsolidationService.BuildIncomeStatementEliminations(
            icTransactions, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.NotNull(result);
        Assert.Equal(-1000m, result!.TotalRevenue);
    }

    [Fact]
    public void BuildIncomeStatementEliminations_NoTransactionsInRange_ReturnsNull()
    {
        var icTransactions = new List<InterCompanyTransaction>
        {
            new()
            {
                Amount = 1000,
                TransactionDate = new DateTime(2026, 6, 15),
                Status = InterCompanyStatus.Posted,
                EliminateOnConsolidation = true
            }
        };

        var result = ConsolidationService.BuildIncomeStatementEliminations(
            icTransactions, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.Null(result);
    }

    // ── Consolidated model properties ──

    [Fact]
    public void ConsolidatedBalanceSheetReport_DefaultProperties()
    {
        var report = new ConsolidatedBalanceSheetReport();
        Assert.Empty(report.CompanyReports);
        Assert.Null(report.Eliminations);
        Assert.NotNull(report.Consolidated);
    }

    [Fact]
    public void ConsolidatedIncomeStatementReport_DefaultProperties()
    {
        var report = new ConsolidatedIncomeStatementReport();
        Assert.Empty(report.CompanyReports);
        Assert.Null(report.Eliminations);
        Assert.NotNull(report.Consolidated);
    }
}
