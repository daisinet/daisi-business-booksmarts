using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class FinancialStatementService(BookSmartsCosmo cosmo, EncryptionContext encryption)
{
    /// <summary>
    /// Generates a balance sheet as of a given date.
    /// </summary>
    public async Task<BalanceSheetReport> GetBalanceSheetAsync(string companyId, DateTime asOfDate, bool cashBasis = false)
    {
        var entries = await cosmo.GetPostedJournalEntriesAsync(companyId, asOfDate, cashBasis ? true : null);
        DecryptAll(entries);
        var accounts = await cosmo.GetChartOfAccountsAsync(companyId, activeOnly: false);
        DecryptAll(accounts);

        var accountMap = accounts.ToDictionary(a => a.id);
        var balances = AggregateBalances(entries, accountMap);

        // Build sections for balance sheet categories
        var assetSections = BuildSections(accounts, balances, AccountCategory.Asset);
        var liabilitySections = BuildSections(accounts, balances, AccountCategory.Liability);

        // Compute retained earnings: Revenue - Expenses (income statement accounts roll into equity)
        var revenueAccounts = accounts.Where(a => a.Category == AccountCategory.Revenue);
        var expenseAccounts = accounts.Where(a => a.Category == AccountCategory.Expense);

        decimal totalRevenue = revenueAccounts.Sum(a => balances.TryGetValue(a.id, out var b) ? b : 0m);
        decimal totalExpenses = expenseAccounts.Sum(a => balances.TryGetValue(a.id, out var b) ? b : 0m);
        decimal retainedEarnings = totalRevenue - totalExpenses;

        // Build equity sections (from actual equity accounts)
        var equitySections = BuildSections(accounts, balances, AccountCategory.Equity);

        // Add computed retained earnings as a line in equity
        // If there's already a RetainedEarnings section, add to it; otherwise create one
        var reSection = equitySections.FirstOrDefault(s => s.SubType == AccountSubType.RetainedEarnings);
        if (reSection == null)
        {
            reSection = new FinancialStatementSection
            {
                Title = GetSubTypeSectionTitle(AccountSubType.RetainedEarnings),
                SubType = AccountSubType.RetainedEarnings
            };
            equitySections.Add(reSection);
        }

        // Add computed retained earnings as a synthetic line (net income rolled in)
        var existingReBalance = reSection.Lines.Sum(l => l.Balance);
        if (retainedEarnings != 0 || existingReBalance == 0)
        {
            reSection.Lines.Add(new FinancialStatementLine
            {
                AccountName = "Net Income (Current Period)",
                Balance = retainedEarnings
            });
        }

        decimal totalAssets = assetSections.Sum(s => s.Total);
        decimal totalLiabilities = liabilitySections.Sum(s => s.Total);
        decimal totalEquity = equitySections.Sum(s => s.Total);

        return new BalanceSheetReport
        {
            AsOfDate = asOfDate,
            CashBasis = cashBasis,
            AssetSections = assetSections,
            LiabilitySections = liabilitySections,
            EquitySections = equitySections,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity,
            RetainedEarnings = retainedEarnings
        };
    }

    /// <summary>
    /// Generates an income statement for a date range.
    /// </summary>
    public async Task<IncomeStatementReport> GetIncomeStatementAsync(
        string companyId, DateTime fromDate, DateTime toDate, bool cashBasis = false)
    {
        var entries = await cosmo.GetPostedJournalEntriesForRangeAsync(companyId, fromDate, toDate, cashBasis ? true : null);
        DecryptAll(entries);
        var accounts = await cosmo.GetChartOfAccountsAsync(companyId, activeOnly: false);
        DecryptAll(accounts);

        var accountMap = accounts.ToDictionary(a => a.id);
        var balances = AggregateBalances(entries, accountMap);

        var revenueSections = BuildSections(accounts, balances, AccountCategory.Revenue);
        var expenseSections = BuildSections(accounts, balances, AccountCategory.Expense);

        return new IncomeStatementReport
        {
            FromDate = fromDate,
            ToDate = toDate,
            CashBasis = cashBasis,
            RevenueSections = revenueSections,
            ExpenseSections = expenseSections,
            TotalRevenue = revenueSections.Sum(s => s.Total),
            TotalExpenses = expenseSections.Sum(s => s.Total)
        };
    }

    /// <summary>
    /// Generates a statement of cash flows using the indirect method.
    /// </summary>
    public async Task<CashFlowReport> GetCashFlowStatementAsync(string companyId, DateTime fromDate, DateTime toDate)
    {
        var accounts = await cosmo.GetChartOfAccountsAsync(companyId, activeOnly: false);
        DecryptAll(accounts);
        var accountMap = accounts.ToDictionary(a => a.id);

        // Get balances at beginning of period (everything before fromDate)
        var beginEntries = await cosmo.GetPostedJournalEntriesAsync(companyId, fromDate.AddDays(-1));
        DecryptAll(beginEntries);
        var beginBalances = AggregateBalances(beginEntries, accountMap);

        // Get balances at end of period (everything up to toDate)
        var endEntries = await cosmo.GetPostedJournalEntriesAsync(companyId, toDate);
        DecryptAll(endEntries);
        var endBalances = AggregateBalances(endEntries, accountMap);

        // Get period income statement for net income
        var periodEntries = await cosmo.GetPostedJournalEntriesForRangeAsync(companyId, fromDate, toDate);
        DecryptAll(periodEntries);
        var periodBalances = AggregateBalances(periodEntries, accountMap);

        decimal netIncome = accounts
            .Where(a => a.Category == AccountCategory.Revenue)
            .Sum(a => periodBalances.TryGetValue(a.id, out var b) ? b : 0m)
            - accounts
            .Where(a => a.Category == AccountCategory.Expense)
            .Sum(a => periodBalances.TryGetValue(a.id, out var b) ? b : 0m);

        // Beginning cash balance
        decimal beginningCash = accounts
            .Where(a => a.SubType is AccountSubType.Cash or AccountSubType.Bank)
            .Sum(a => beginBalances.TryGetValue(a.id, out var b) ? b : 0m);

        // Operating activities (indirect method)
        var operating = new List<CashFlowItem>
        {
            new() { Label = "Net Income", Amount = netIncome }
        };

        // Working capital changes (current assets except cash, and current liabilities)
        foreach (var account in accounts.OrderBy(a => a.AccountNumber))
        {
            if (account.SubType is AccountSubType.Cash or AccountSubType.Bank)
                continue; // cash accounts are not adjustments

            decimal beginBal = beginBalances.TryGetValue(account.id, out var bb) ? bb : 0m;
            decimal endBal = endBalances.TryGetValue(account.id, out var eb) ? eb : 0m;
            decimal change = endBal - beginBal;

            if (change == 0) continue;

            if (IsCurrentAsset(account.SubType))
            {
                // Increase in current asset = cash outflow (negative)
                operating.Add(new CashFlowItem
                {
                    Label = $"Change in {account.Name}",
                    AccountNumber = account.AccountNumber,
                    Amount = -change
                });
            }
            else if (IsCurrentLiability(account.SubType))
            {
                // Increase in current liability = cash inflow (positive)
                operating.Add(new CashFlowItem
                {
                    Label = $"Change in {account.Name}",
                    AccountNumber = account.AccountNumber,
                    Amount = change
                });
            }
        }

        // Investing activities (fixed/long-term assets)
        var investing = new List<CashFlowItem>();
        foreach (var account in accounts.Where(a => IsLongTermAsset(a.SubType)).OrderBy(a => a.AccountNumber))
        {
            decimal beginBal = beginBalances.TryGetValue(account.id, out var bb) ? bb : 0m;
            decimal endBal = endBalances.TryGetValue(account.id, out var eb) ? eb : 0m;
            decimal change = endBal - beginBal;

            if (change == 0) continue;

            // Increase in long-term assets = cash outflow (negative)
            investing.Add(new CashFlowItem
            {
                Label = $"Change in {account.Name}",
                AccountNumber = account.AccountNumber,
                Amount = -change
            });
        }

        // Financing activities (equity accounts and long-term debt, excluding retained earnings)
        var financing = new List<CashFlowItem>();
        foreach (var account in accounts
            .Where(a => IsFinancingAccount(a))
            .OrderBy(a => a.AccountNumber))
        {
            decimal beginBal = beginBalances.TryGetValue(account.id, out var bb) ? bb : 0m;
            decimal endBal = endBalances.TryGetValue(account.id, out var eb) ? eb : 0m;
            decimal change = endBal - beginBal;

            if (change == 0) continue;

            financing.Add(new CashFlowItem
            {
                Label = $"Change in {account.Name}",
                AccountNumber = account.AccountNumber,
                Amount = change
            });
        }

        decimal netOperating = operating.Sum(i => i.Amount);
        decimal netInvesting = investing.Sum(i => i.Amount);
        decimal netFinancing = financing.Sum(i => i.Amount);

        return new CashFlowReport
        {
            FromDate = fromDate,
            ToDate = toDate,
            BeginningCashBalance = beginningCash,
            OperatingActivities = operating,
            InvestingActivities = investing,
            FinancingActivities = financing,
            NetOperating = netOperating,
            NetInvesting = netInvesting,
            NetFinancing = netFinancing
        };
    }

    // ── Helpers ──

    /// <summary>
    /// Aggregates journal entry lines into net balances per account, with correct sign per normal balance convention.
    /// </summary>
    private static Dictionary<string, decimal> AggregateBalances(
        List<JournalEntry> entries, Dictionary<string, ChartOfAccountEntry> accountMap)
    {
        var raw = new Dictionary<string, (decimal debit, decimal credit)>();

        foreach (var entry in entries)
        {
            foreach (var line in entry.Lines)
            {
                if (!raw.ContainsKey(line.AccountId))
                    raw[line.AccountId] = (0, 0);

                var current = raw[line.AccountId];
                raw[line.AccountId] = (current.debit + line.Debit, current.credit + line.Credit);
            }
        }

        var balances = new Dictionary<string, decimal>();
        foreach (var (accountId, (debit, credit)) in raw)
        {
            if (accountMap.TryGetValue(accountId, out var account))
                balances[accountId] = ComputeNetBalance(account.Category, debit, credit);
        }

        return balances;
    }

    /// <summary>
    /// Computes net balance with correct sign per normal balance convention.
    /// Assets/Expenses are debit-normal (positive = debit > credit).
    /// Liabilities/Equity/Revenue are credit-normal (positive = credit > debit).
    /// </summary>
    public static decimal ComputeNetBalance(AccountCategory category, decimal debit, decimal credit)
    {
        return category switch
        {
            AccountCategory.Asset or AccountCategory.Expense => debit - credit,
            _ => credit - debit // Liability, Equity, Revenue
        };
    }

    /// <summary>
    /// Builds grouped sections for a given account category from net balances.
    /// </summary>
    private static List<FinancialStatementSection> BuildSections(
        List<ChartOfAccountEntry> accounts,
        Dictionary<string, decimal> balances,
        AccountCategory category)
    {
        var sections = new List<FinancialStatementSection>();

        var categoryAccounts = accounts
            .Where(a => a.Category == category && balances.ContainsKey(a.id))
            .OrderBy(a => a.AccountNumber)
            .GroupBy(a => a.SubType);

        foreach (var group in categoryAccounts.OrderBy(g => (int)g.Key))
        {
            var section = new FinancialStatementSection
            {
                Title = GetSubTypeSectionTitle(group.Key),
                SubType = group.Key,
                Lines = group.Select(a => new FinancialStatementLine
                {
                    AccountId = a.id,
                    AccountNumber = a.AccountNumber,
                    AccountName = a.Name,
                    Balance = balances[a.id]
                }).ToList()
            };

            sections.Add(section);
        }

        return sections;
    }

    /// <summary>
    /// Maps an AccountSubType to a human-readable section title.
    /// </summary>
    public static string GetSubTypeSectionTitle(AccountSubType subType) => subType switch
    {
        // Assets
        AccountSubType.Cash => "Cash & Bank",
        AccountSubType.Bank => "Cash & Bank",
        AccountSubType.AccountsReceivable => "Accounts Receivable",
        AccountSubType.Inventory => "Inventory",
        AccountSubType.PrepaidExpenses => "Prepaid Expenses",
        AccountSubType.OtherCurrentAsset => "Other Current Assets",
        AccountSubType.InterCompanyReceivable => "Inter-Company Receivable",
        AccountSubType.FixedAsset => "Fixed Assets",
        AccountSubType.AccumulatedDepreciation => "Accumulated Depreciation",
        AccountSubType.OtherAsset => "Other Assets",

        // Liabilities
        AccountSubType.AccountsPayable => "Accounts Payable",
        AccountSubType.CreditCard => "Credit Cards",
        AccountSubType.AccruedLiabilities => "Accrued Liabilities",
        AccountSubType.CurrentPortionLongTermDebt => "Current Portion of Long-Term Debt",
        AccountSubType.OtherCurrentLiability => "Other Current Liabilities",
        AccountSubType.InterCompanyPayable => "Inter-Company Payable",
        AccountSubType.LongTermDebt => "Long-Term Debt",
        AccountSubType.OtherLiability => "Other Liabilities",

        // Equity
        AccountSubType.OwnersEquity => "Owner's Equity",
        AccountSubType.RetainedEarnings => "Retained Earnings",
        AccountSubType.CommonStock => "Common Stock",
        AccountSubType.AdditionalPaidInCapital => "Additional Paid-In Capital",
        AccountSubType.OtherEquity => "Other Equity",

        // Revenue
        AccountSubType.SalesRevenue => "Sales Revenue",
        AccountSubType.ServiceRevenue => "Service Revenue",
        AccountSubType.OtherRevenue => "Other Revenue",
        AccountSubType.InterestIncome => "Interest Income",

        // Expenses
        AccountSubType.CostOfGoodsSold => "Cost of Goods Sold",
        AccountSubType.Payroll => "Payroll",
        AccountSubType.Rent => "Rent",
        AccountSubType.Utilities => "Utilities",
        AccountSubType.Insurance => "Insurance",
        AccountSubType.Depreciation => "Depreciation",
        AccountSubType.OfficeExpenses => "Office Expenses",
        AccountSubType.TravelExpenses => "Travel Expenses",
        AccountSubType.ProfessionalFees => "Professional Fees",
        AccountSubType.InterestExpense => "Interest Expense",
        AccountSubType.TaxExpense => "Tax Expense",
        AccountSubType.OtherExpense => "Other Expenses",

        _ => subType.ToString()
    };

    private static bool IsCurrentAsset(AccountSubType subType) =>
        subType is AccountSubType.AccountsReceivable
            or AccountSubType.Inventory
            or AccountSubType.PrepaidExpenses
            or AccountSubType.OtherCurrentAsset
            or AccountSubType.InterCompanyReceivable;

    private static bool IsCurrentLiability(AccountSubType subType) =>
        subType is AccountSubType.AccountsPayable
            or AccountSubType.CreditCard
            or AccountSubType.AccruedLiabilities
            or AccountSubType.CurrentPortionLongTermDebt
            or AccountSubType.OtherCurrentLiability
            or AccountSubType.InterCompanyPayable;

    private static bool IsLongTermAsset(AccountSubType subType) =>
        subType is AccountSubType.FixedAsset
            or AccountSubType.AccumulatedDepreciation
            or AccountSubType.OtherAsset;

    private static bool IsFinancingAccount(ChartOfAccountEntry account) =>
        (account.Category == AccountCategory.Equity && account.SubType != AccountSubType.RetainedEarnings)
        || account.SubType == AccountSubType.LongTermDebt
        || account.SubType == AccountSubType.OtherLiability;

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
