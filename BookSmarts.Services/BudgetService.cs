using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class BudgetService(BookSmartsCosmo cosmo, EncryptionContext encryption, FinancialStatementService financials, PeriodService periods)
{
    public async Task<Budget> CreateBudgetAsync(Budget budget)
    {
        ValidateBudget(budget);
        Encrypt(budget);
        return Decrypt(await cosmo.CreateBudgetAsync(budget));
    }

    public async Task<Budget?> GetBudgetAsync(string id, string companyId)
    {
        var budget = await cosmo.GetBudgetAsync(id, companyId);
        return budget != null ? Decrypt(budget) : null;
    }

    public async Task<List<Budget>> GetBudgetsAsync(string companyId)
    {
        var budgets = await cosmo.GetBudgetsAsync(companyId);
        return DecryptAll(budgets);
    }

    public async Task<Budget> UpdateBudgetAsync(Budget budget)
    {
        ValidateBudget(budget);
        Encrypt(budget);
        return Decrypt(await cosmo.UpdateBudgetAsync(budget));
    }

    public async Task DeleteBudgetAsync(string id, string companyId)
    {
        await cosmo.DeleteBudgetAsync(id, companyId);
    }

    /// <summary>
    /// Transitions a budget from Draft to Approved.
    /// </summary>
    public async Task<Budget> ApproveBudgetAsync(string id, string companyId)
    {
        var budget = Decrypt(await cosmo.GetBudgetAsync(id, companyId)
            ?? throw new InvalidOperationException("Budget not found."));

        if (budget.Status != BudgetStatus.Draft)
            throw new InvalidOperationException("Only draft budgets can be approved.");

        budget.Status = BudgetStatus.Approved;
        Encrypt(budget);
        return Decrypt(await cosmo.UpdateBudgetAsync(budget));
    }

    /// <summary>
    /// Generates a Budget vs Actual report for the given budget and date range.
    /// </summary>
    public async Task<BudgetVsActualReport> GetBudgetVsActualAsync(
        string companyId, string budgetId, DateTime fromDate, DateTime toDate, bool cashBasis = false)
    {
        var budget = Decrypt(await cosmo.GetBudgetAsync(budgetId, companyId)
            ?? throw new InvalidOperationException("Budget not found."));

        var actuals = await financials.GetIncomeStatementAsync(companyId, fromDate, toDate, cashBasis);

        var fiscalYear = await periods.GetFiscalYearAsync(budget.FiscalYearId, companyId);
        var fiscalPeriods = fiscalYear?.Periods ?? new();

        return BuildBudgetVsActual(budget, actuals, fiscalPeriods, fromDate, toDate);
    }

    /// <summary>
    /// Pure logic: builds a BvA report from budget, actual income statement, and fiscal periods.
    /// </summary>
    public static BudgetVsActualReport BuildBudgetVsActual(
        Budget budget, IncomeStatementReport actuals, List<FiscalPeriod> fiscalPeriods,
        DateTime fromDate, DateTime toDate)
    {
        // Filter budget lines to periods that overlap with the date range
        var overlappingPeriodIds = fiscalPeriods
            .Where(p => p.StartDate <= toDate && p.EndDate >= fromDate)
            .Select(p => p.PeriodId)
            .ToHashSet();

        var filteredLines = budget.LineItems
            .Where(l => overlappingPeriodIds.Contains(l.PeriodId))
            .ToList();

        // Sum budget amounts per account
        var budgetByAccount = filteredLines
            .GroupBy(l => l.AccountId)
            .ToDictionary(g => g.Key, g => (
                Amount: g.Sum(l => l.Amount),
                Number: g.First().AccountNumber,
                Name: g.First().AccountName
            ));

        // Build actuals lookup from income statement sections
        var actualByAccount = new Dictionary<string, (decimal Amount, string Number, string Name, string SectionTitle)>();
        foreach (var section in actuals.RevenueSections.Concat(actuals.ExpenseSections))
        {
            foreach (var line in section.Lines)
            {
                actualByAccount[line.AccountId] = (line.Balance, line.AccountNumber, line.AccountName, section.Title);
            }
        }

        // Collect all account IDs and group by section
        var allAccountIds = budgetByAccount.Keys.Union(actualByAccount.Keys).ToList();

        var revenueSections = new Dictionary<string, List<BudgetVsActualLine>>();
        var expenseSections = new Dictionary<string, List<BudgetVsActualLine>>();

        foreach (var accountId in allAccountIds)
        {
            var budgetAmount = budgetByAccount.TryGetValue(accountId, out var b) ? b.Amount : 0m;
            var actualAmount = actualByAccount.TryGetValue(accountId, out var a) ? a.Amount : 0m;
            var accountNumber = a.Number ?? b.Number ?? "";
            var accountName = a.Name ?? b.Name ?? "";

            var line = new BudgetVsActualLine
            {
                AccountId = accountId,
                AccountNumber = accountNumber,
                AccountName = accountName,
                BudgetedAmount = budgetAmount,
                ActualAmount = actualAmount
            };

            // Determine if revenue or expense by section membership
            string sectionTitle;
            bool isRevenue;

            if (actualByAccount.TryGetValue(accountId, out var actual))
            {
                sectionTitle = actual.SectionTitle;
                isRevenue = actuals.RevenueSections.Any(s => s.Title == sectionTitle);
            }
            else
            {
                // Budget-only account — infer from account number convention
                sectionTitle = accountNumber.StartsWith("4") ? "Revenue" : "Expenses";
                isRevenue = accountNumber.StartsWith("4");
            }

            var targetSections = isRevenue ? revenueSections : expenseSections;
            if (!targetSections.ContainsKey(sectionTitle))
                targetSections[sectionTitle] = new();
            targetSections[sectionTitle].Add(line);
        }

        var revSections = revenueSections.Select(kvp => new BudgetVsActualSection
        {
            Title = kvp.Key,
            Lines = kvp.Value.OrderBy(l => l.AccountNumber).ToList()
        }).ToList();

        var expSections = expenseSections.Select(kvp => new BudgetVsActualSection
        {
            Title = kvp.Key,
            Lines = kvp.Value.OrderBy(l => l.AccountNumber).ToList()
        }).ToList();

        return new BudgetVsActualReport
        {
            BudgetName = budget.Name,
            FromDate = fromDate,
            ToDate = toDate,
            RevenueSections = revSections,
            ExpenseSections = expSections,
            TotalBudgetedRevenue = revSections.Sum(s => s.TotalBudgeted),
            TotalActualRevenue = revSections.Sum(s => s.TotalActual),
            TotalBudgetedExpenses = expSections.Sum(s => s.TotalBudgeted),
            TotalActualExpenses = expSections.Sum(s => s.TotalActual)
        };
    }

    /// <summary>
    /// Validates a budget has all required fields.
    /// </summary>
    public static void ValidateBudget(Budget budget)
    {
        if (string.IsNullOrEmpty(budget.CompanyId))
            throw new InvalidOperationException("Company ID is required.");

        if (string.IsNullOrEmpty(budget.FiscalYearId))
            throw new InvalidOperationException("Fiscal Year ID is required.");

        if (string.IsNullOrEmpty(budget.Name))
            throw new InvalidOperationException("Budget name is required.");

        foreach (var item in budget.LineItems)
        {
            if (string.IsNullOrEmpty(item.AccountId))
                throw new InvalidOperationException("All line items must have an Account ID.");

            if (string.IsNullOrEmpty(item.PeriodId))
                throw new InvalidOperationException("All line items must have a Period ID.");

            if (item.Amount < 0)
                throw new InvalidOperationException("Budget amounts must be non-negative.");
        }
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
