using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;

namespace BookSmarts.Services;

public class ConsolidationService(
    FinancialStatementService financials,
    OrganizationService organizations,
    InterCompanyService interCompany)
{
    /// <summary>
    /// Generates a consolidated balance sheet across all companies in an organization.
    /// </summary>
    public async Task<ConsolidatedBalanceSheetReport> GetConsolidatedBalanceSheetAsync(
        string accountId, string organizationId, DateTime asOfDate, bool cashBasis = false)
    {
        var org = await organizations.GetOrganizationAsync(organizationId, accountId)
            ?? throw new InvalidOperationException("Organization not found.");

        var companies = await organizations.GetCompaniesAsync(accountId, organizationId);

        // Get balance sheet for each company in parallel
        var tasks = companies.Select(async c =>
        {
            var report = await financials.GetBalanceSheetAsync(c.id, asOfDate, cashBasis);
            return new CompanyBalanceSheet
            {
                CompanyId = c.id,
                CompanyName = c.Name,
                Report = report
            };
        });

        var companyReports = (await Task.WhenAll(tasks)).ToList();

        // Get IC transactions for elimination
        var icTransactions = await interCompany.GetPostedInterCompanyTransactionsAsync(organizationId, asOfDate);

        // Merge all balance sheets
        var allReports = companyReports.Select(cr => cr.Report).ToList();
        var merged = MergeBalanceSheets(allReports);

        // Build eliminations
        var eliminations = BuildBalanceSheetEliminations(icTransactions, asOfDate);

        // Apply eliminations to merged
        if (eliminations != null)
        {
            ApplyBalanceSheetEliminations(merged, eliminations);
        }

        return new ConsolidatedBalanceSheetReport
        {
            OrganizationName = org.Name,
            AsOfDate = asOfDate,
            CashBasis = cashBasis,
            CompanyReports = companyReports,
            Consolidated = merged,
            Eliminations = eliminations
        };
    }

    /// <summary>
    /// Generates a consolidated income statement across all companies in an organization.
    /// </summary>
    public async Task<ConsolidatedIncomeStatementReport> GetConsolidatedIncomeStatementAsync(
        string accountId, string organizationId, DateTime fromDate, DateTime toDate, bool cashBasis = false)
    {
        var org = await organizations.GetOrganizationAsync(organizationId, accountId)
            ?? throw new InvalidOperationException("Organization not found.");

        var companies = await organizations.GetCompaniesAsync(accountId, organizationId);

        var tasks = companies.Select(async c =>
        {
            var report = await financials.GetIncomeStatementAsync(c.id, fromDate, toDate, cashBasis);
            return new CompanyIncomeStatement
            {
                CompanyId = c.id,
                CompanyName = c.Name,
                Report = report
            };
        });

        var companyReports = (await Task.WhenAll(tasks)).ToList();

        var icTransactions = await interCompany.GetPostedInterCompanyTransactionsAsync(organizationId);

        var allReports = companyReports.Select(cr => cr.Report).ToList();
        var merged = MergeIncomeStatements(allReports);

        var eliminations = BuildIncomeStatementEliminations(icTransactions, fromDate, toDate);

        if (eliminations != null)
        {
            ApplyIncomeStatementEliminations(merged, eliminations);
        }

        return new ConsolidatedIncomeStatementReport
        {
            OrganizationName = org.Name,
            FromDate = fromDate,
            ToDate = toDate,
            CashBasis = cashBasis,
            CompanyReports = companyReports,
            Consolidated = merged,
            Eliminations = eliminations
        };
    }

    /// <summary>
    /// Merges multiple balance sheet reports into one by summing balances per SubType section.
    /// </summary>
    public static BalanceSheetReport MergeBalanceSheets(List<BalanceSheetReport> reports)
    {
        if (reports.Count == 0)
            return new BalanceSheetReport();

        var assetSections = MergeSections(reports.SelectMany(r => r.AssetSections).ToList());
        var liabilitySections = MergeSections(reports.SelectMany(r => r.LiabilitySections).ToList());
        var equitySections = MergeSections(reports.SelectMany(r => r.EquitySections).ToList());

        var totalAssets = assetSections.Sum(s => s.Total);
        var totalLiabilities = liabilitySections.Sum(s => s.Total);
        var totalEquity = equitySections.Sum(s => s.Total);

        return new BalanceSheetReport
        {
            AsOfDate = reports.First().AsOfDate,
            CashBasis = reports.First().CashBasis,
            AssetSections = assetSections,
            LiabilitySections = liabilitySections,
            EquitySections = equitySections,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity,
            RetainedEarnings = reports.Sum(r => r.RetainedEarnings)
        };
    }

    /// <summary>
    /// Merges multiple income statement reports into one.
    /// </summary>
    public static IncomeStatementReport MergeIncomeStatements(List<IncomeStatementReport> reports)
    {
        if (reports.Count == 0)
            return new IncomeStatementReport();

        var revenueSections = MergeSections(reports.SelectMany(r => r.RevenueSections).ToList());
        var expenseSections = MergeSections(reports.SelectMany(r => r.ExpenseSections).ToList());

        return new IncomeStatementReport
        {
            FromDate = reports.First().FromDate,
            ToDate = reports.First().ToDate,
            CashBasis = reports.First().CashBasis,
            RevenueSections = revenueSections,
            ExpenseSections = expenseSections,
            TotalRevenue = revenueSections.Sum(s => s.Total),
            TotalExpenses = expenseSections.Sum(s => s.Total)
        };
    }

    /// <summary>
    /// Builds a balance sheet representing IC eliminations (offsetting entries for IC receivables/payables).
    /// Returns null if no eliminations needed.
    /// </summary>
    public static BalanceSheetReport? BuildBalanceSheetEliminations(
        List<InterCompanyTransaction> icTransactions, DateTime asOfDate)
    {
        var eligible = icTransactions
            .Where(t => t.EliminateOnConsolidation && t.Status == InterCompanyStatus.Posted && t.TransactionDate <= asOfDate)
            .ToList();

        if (eligible.Count == 0)
            return null;

        var totalElimination = eligible.Sum(t => t.Amount);

        // IC Receivable elimination (reduce assets)
        var assetElimination = new FinancialStatementSection
        {
            Title = "IC Receivable Elimination",
            SubType = AccountSubType.InterCompanyReceivable,
            Lines = new()
            {
                new FinancialStatementLine
                {
                    AccountName = "Elimination: IC Receivable",
                    Balance = -totalElimination
                }
            }
        };

        // IC Payable elimination (reduce liabilities)
        var liabilityElimination = new FinancialStatementSection
        {
            Title = "IC Payable Elimination",
            SubType = AccountSubType.InterCompanyPayable,
            Lines = new()
            {
                new FinancialStatementLine
                {
                    AccountName = "Elimination: IC Payable",
                    Balance = -totalElimination
                }
            }
        };

        return new BalanceSheetReport
        {
            AsOfDate = asOfDate,
            AssetSections = new() { assetElimination },
            LiabilitySections = new() { liabilityElimination },
            TotalAssets = -totalElimination,
            TotalLiabilities = -totalElimination
        };
    }

    /// <summary>
    /// Builds an income statement representing IC eliminations for revenue/expense.
    /// Returns null if no eliminations needed.
    /// </summary>
    public static IncomeStatementReport? BuildIncomeStatementEliminations(
        List<InterCompanyTransaction> icTransactions, DateTime fromDate, DateTime toDate)
    {
        var eligible = icTransactions
            .Where(t => t.EliminateOnConsolidation && t.Status == InterCompanyStatus.Posted
                && t.TransactionDate >= fromDate && t.TransactionDate <= toDate)
            .ToList();

        if (eligible.Count == 0)
            return null;

        var totalElimination = eligible.Sum(t => t.Amount);

        var revenueElimination = new FinancialStatementSection
        {
            Title = "IC Revenue Elimination",
            Lines = new()
            {
                new FinancialStatementLine
                {
                    AccountName = "Elimination: IC Revenue",
                    Balance = -totalElimination
                }
            }
        };

        var expenseElimination = new FinancialStatementSection
        {
            Title = "IC Expense Elimination",
            Lines = new()
            {
                new FinancialStatementLine
                {
                    AccountName = "Elimination: IC Expense",
                    Balance = -totalElimination
                }
            }
        };

        return new IncomeStatementReport
        {
            FromDate = fromDate,
            ToDate = toDate,
            RevenueSections = new() { revenueElimination },
            ExpenseSections = new() { expenseElimination },
            TotalRevenue = -totalElimination,
            TotalExpenses = -totalElimination
        };
    }

    // ── Private helpers ──

    private static List<FinancialStatementSection> MergeSections(List<FinancialStatementSection> sections)
    {
        return sections
            .GroupBy(s => s.Title)
            .Select(g =>
            {
                var allLines = g.SelectMany(s => s.Lines).ToList();

                // Merge lines with the same AccountId by summing balances
                var mergedLines = allLines
                    .GroupBy(l => string.IsNullOrEmpty(l.AccountId) ? Guid.NewGuid().ToString() : l.AccountId)
                    .Select(lg => new FinancialStatementLine
                    {
                        AccountId = lg.First().AccountId,
                        AccountNumber = lg.First().AccountNumber,
                        AccountName = lg.First().AccountName,
                        Balance = lg.Sum(l => l.Balance)
                    })
                    .OrderBy(l => l.AccountNumber)
                    .ToList();

                return new FinancialStatementSection
                {
                    Title = g.Key,
                    SubType = g.First().SubType,
                    Lines = mergedLines
                };
            })
            .OrderBy(s => s.SubType.HasValue ? (int)s.SubType.Value : int.MaxValue)
            .ToList();
    }

    private static void ApplyBalanceSheetEliminations(BalanceSheetReport merged, BalanceSheetReport eliminations)
    {
        merged.TotalAssets += eliminations.TotalAssets;
        merged.TotalLiabilities += eliminations.TotalLiabilities;
        merged.TotalEquity += eliminations.TotalEquity;
    }

    private static void ApplyIncomeStatementEliminations(IncomeStatementReport merged, IncomeStatementReport eliminations)
    {
        merged.TotalRevenue += eliminations.TotalRevenue;
        merged.TotalExpenses += eliminations.TotalExpenses;
    }
}
