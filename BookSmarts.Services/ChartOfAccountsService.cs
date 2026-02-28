using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class ChartOfAccountsService(BookSmartsCosmo cosmo, EncryptionContext encryption)
{
    public async Task<List<ChartOfAccountEntry>> GetChartOfAccountsAsync(string companyId, bool activeOnly = true)
    {
        var entries = await cosmo.GetChartOfAccountsAsync(companyId, activeOnly);
        return DecryptAll(entries);
    }

    public async Task<ChartOfAccountEntry?> GetAccountEntryAsync(string id, string companyId)
    {
        var entry = await cosmo.GetAccountEntryAsync(id, companyId);
        return entry != null ? Decrypt(entry) : null;
    }

    public async Task<ChartOfAccountEntry> CreateAccountEntryAsync(ChartOfAccountEntry entry)
    {
        if (await cosmo.AccountNumberExistsAsync(entry.CompanyId, entry.AccountNumber))
            throw new InvalidOperationException($"Account number '{entry.AccountNumber}' already exists.");

        entry.NormalBalance = GetNormalBalance(entry.Category);
        Encrypt(entry);
        return Decrypt(await cosmo.CreateAccountAsync(entry));
    }

    public async Task<ChartOfAccountEntry> UpdateAccountEntryAsync(ChartOfAccountEntry entry)
    {
        if (await cosmo.AccountNumberExistsAsync(entry.CompanyId, entry.AccountNumber, entry.id))
            throw new InvalidOperationException($"Account number '{entry.AccountNumber}' already exists.");

        Encrypt(entry);
        return Decrypt(await cosmo.UpdateAccountEntryAsync(entry));
    }

    public static NormalBalance GetNormalBalance(AccountCategory category)
    {
        return category switch
        {
            AccountCategory.Asset => NormalBalance.Debit,
            AccountCategory.Expense => NormalBalance.Debit,
            AccountCategory.Liability => NormalBalance.Credit,
            AccountCategory.Equity => NormalBalance.Credit,
            AccountCategory.Revenue => NormalBalance.Credit,
            _ => NormalBalance.Debit
        };
    }

    /// <summary>
    /// Seeds a company with a standard US GAAP chart of accounts.
    /// </summary>
    public async Task SeedDefaultChartOfAccountsAsync(string companyId)
    {
        var accounts = GetDefaultChartOfAccounts(companyId);
        foreach (var account in accounts)
        {
            Encrypt(account);
            await cosmo.CreateAccountAsync(account);
        }
    }

    public static List<ChartOfAccountEntry> GetDefaultChartOfAccounts(string companyId)
    {
        return
        [
            // Assets
            MakeEntry(companyId, "1000", "Cash", AccountCategory.Asset, AccountSubType.Cash, true),
            MakeEntry(companyId, "1010", "Checking Account", AccountCategory.Asset, AccountSubType.Bank),
            MakeEntry(companyId, "1020", "Savings Account", AccountCategory.Asset, AccountSubType.Bank),
            MakeEntry(companyId, "1100", "Accounts Receivable", AccountCategory.Asset, AccountSubType.AccountsReceivable, true),
            MakeEntry(companyId, "1200", "Inventory", AccountCategory.Asset, AccountSubType.Inventory),
            MakeEntry(companyId, "1050", "Short-Term Investments", AccountCategory.Asset, AccountSubType.OtherCurrentAsset),
            MakeEntry(companyId, "1300", "Prepaid Expenses", AccountCategory.Asset, AccountSubType.PrepaidExpenses),
            MakeEntry(companyId, "1400", "Long-Term Investments", AccountCategory.Asset, AccountSubType.OtherAsset),
            MakeEntry(companyId, "1500", "Furniture & Equipment", AccountCategory.Asset, AccountSubType.FixedAsset),
            MakeEntry(companyId, "1510", "Accumulated Depreciation", AccountCategory.Asset, AccountSubType.AccumulatedDepreciation),

            // Liabilities
            MakeEntry(companyId, "2000", "Accounts Payable", AccountCategory.Liability, AccountSubType.AccountsPayable, true),
            MakeEntry(companyId, "2100", "Credit Card", AccountCategory.Liability, AccountSubType.CreditCard),
            MakeEntry(companyId, "2200", "Accrued Liabilities", AccountCategory.Liability, AccountSubType.AccruedLiabilities),
            MakeEntry(companyId, "2300", "Sales Tax Payable", AccountCategory.Liability, AccountSubType.OtherCurrentLiability),
            MakeEntry(companyId, "2400", "Payroll Liabilities", AccountCategory.Liability, AccountSubType.OtherCurrentLiability),
            MakeEntry(companyId, "2500", "Long-Term Debt", AccountCategory.Liability, AccountSubType.LongTermDebt),

            // Equity
            MakeEntry(companyId, "3000", "Owner's Equity", AccountCategory.Equity, AccountSubType.OwnersEquity, true),
            MakeEntry(companyId, "3100", "Retained Earnings", AccountCategory.Equity, AccountSubType.RetainedEarnings, true),
            MakeEntry(companyId, "3200", "Owner's Draw", AccountCategory.Equity, AccountSubType.OtherEquity),

            // Revenue
            MakeEntry(companyId, "4000", "Sales Revenue", AccountCategory.Revenue, AccountSubType.SalesRevenue),
            MakeEntry(companyId, "4100", "Service Revenue", AccountCategory.Revenue, AccountSubType.ServiceRevenue),
            MakeEntry(companyId, "4200", "Other Revenue", AccountCategory.Revenue, AccountSubType.OtherRevenue),
            MakeEntry(companyId, "4300", "Interest Income", AccountCategory.Revenue, AccountSubType.InterestIncome),

            // Expenses
            MakeEntry(companyId, "5000", "Cost of Goods Sold", AccountCategory.Expense, AccountSubType.CostOfGoodsSold),
            MakeEntry(companyId, "6000", "Payroll Expense", AccountCategory.Expense, AccountSubType.Payroll),
            MakeEntry(companyId, "6100", "Rent Expense", AccountCategory.Expense, AccountSubType.Rent),
            MakeEntry(companyId, "6200", "Utilities Expense", AccountCategory.Expense, AccountSubType.Utilities),
            MakeEntry(companyId, "6300", "Insurance Expense", AccountCategory.Expense, AccountSubType.Insurance),
            MakeEntry(companyId, "6400", "Office Supplies", AccountCategory.Expense, AccountSubType.OfficeExpenses),
            MakeEntry(companyId, "6500", "Travel & Entertainment", AccountCategory.Expense, AccountSubType.TravelExpenses),
            MakeEntry(companyId, "6600", "Professional Fees", AccountCategory.Expense, AccountSubType.ProfessionalFees),
            MakeEntry(companyId, "6700", "Depreciation Expense", AccountCategory.Expense, AccountSubType.Depreciation),
            MakeEntry(companyId, "6800", "Interest Expense", AccountCategory.Expense, AccountSubType.InterestExpense),
            MakeEntry(companyId, "6900", "Tax Expense", AccountCategory.Expense, AccountSubType.TaxExpense),
            MakeEntry(companyId, "7000", "Other Expense", AccountCategory.Expense, AccountSubType.OtherExpense),
        ];
    }

    private static ChartOfAccountEntry MakeEntry(string companyId, string number, string name, AccountCategory category, AccountSubType subType, bool isSystem = false)
    {
        return new ChartOfAccountEntry
        {
            id = BookSmartsCosmo.GenerateId("coa"),
            CompanyId = companyId,
            AccountNumber = number,
            Name = name,
            Category = category,
            SubType = subType,
            NormalBalance = GetNormalBalance(category),
            IsSystemAccount = isSystem,
            IsActive = true
        };
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
