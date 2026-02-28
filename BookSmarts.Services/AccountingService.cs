using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class AccountingService(BookSmartsCosmo cosmo, EncryptionContext encryption)
{
    /// <summary>
    /// Creates a new journal entry in Draft status after validation.
    /// </summary>
    public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry)
    {
        ValidateJournalEntry(entry);

        var nextNumber = await cosmo.GetNextEntryNumberAsync(entry.CompanyId);
        entry.EntryNumber = $"JE-{nextNumber:D6}";
        entry.Status = JournalEntryStatus.Draft;

        // Number the lines
        for (int i = 0; i < entry.Lines.Count; i++)
            entry.Lines[i].LineNumber = i + 1;

        Encrypt(entry);
        return Decrypt(await cosmo.CreateJournalEntryAsync(entry));
    }

    /// <summary>
    /// Posts a draft journal entry, making it part of the ledger.
    /// </summary>
    public async Task<JournalEntry> PostJournalEntryAsync(string id, string companyId, string? postedBy = null)
    {
        var entry = Decrypt(await cosmo.GetJournalEntryAsync(id, companyId)
            ?? throw new InvalidOperationException("Journal entry not found."));

        if (entry.Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException($"Cannot post a journal entry with status '{entry.Status}'. Only Draft entries can be posted.");

        ValidateJournalEntry(entry);

        // Check fiscal period is open
        if (!string.IsNullOrEmpty(entry.FiscalPeriodId))
        {
            var fy = await cosmo.GetFiscalYearForDateAsync(companyId, entry.EntryDate);
            if (fy != null)
            {
                Decrypt(fy);
                var period = fy.Periods.FirstOrDefault(p => p.PeriodId == entry.FiscalPeriodId);
                if (period?.Status != FiscalPeriodStatus.Open)
                    throw new InvalidOperationException("Cannot post to a closed fiscal period.");
            }
        }

        await cosmo.PatchJournalEntryStatusAsync(id, companyId, JournalEntryStatus.Posted, postedBy);

        entry.Status = JournalEntryStatus.Posted;
        entry.PostedUtc = DateTime.UtcNow;
        entry.PostedBy = postedBy;
        return entry;
    }

    /// <summary>
    /// Voids a posted journal entry. The entry remains but no longer affects balances.
    /// </summary>
    public async Task<JournalEntry> VoidJournalEntryAsync(string id, string companyId)
    {
        var entry = Decrypt(await cosmo.GetJournalEntryAsync(id, companyId)
            ?? throw new InvalidOperationException("Journal entry not found."));

        if (entry.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Only posted journal entries can be voided.");

        await cosmo.PatchJournalEntryStatusAsync(id, companyId, JournalEntryStatus.Voided);

        entry.Status = JournalEntryStatus.Voided;
        entry.VoidedUtc = DateTime.UtcNow;
        return entry;
    }

    /// <summary>
    /// Creates a reversing entry for a posted journal entry.
    /// </summary>
    public async Task<JournalEntry> ReverseJournalEntryAsync(string id, string companyId, DateTime reversalDate, string? createdBy = null)
    {
        var original = Decrypt(await cosmo.GetJournalEntryAsync(id, companyId)
            ?? throw new InvalidOperationException("Journal entry not found."));

        if (original.Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Only posted journal entries can be reversed.");

        // Create a new entry with debits and credits swapped
        var reversal = new JournalEntry
        {
            CompanyId = companyId,
            EntryDate = reversalDate,
            Description = $"Reversal of {original.EntryNumber}",
            Memo = original.Memo,
            SourceType = SourceType.Reversal,
            SourceId = original.id,
            ReversalOfId = original.id,
            FiscalPeriodId = original.FiscalPeriodId,
            CreatedBy = createdBy,
            Lines = original.Lines.Select(l => new JournalLine
            {
                AccountId = l.AccountId,
                AccountNumber = l.AccountNumber,
                AccountName = l.AccountName,
                Debit = l.Credit,
                Credit = l.Debit,
                Description = $"Reversal: {l.Description}"
            }).ToList()
        };

        var created = await CreateJournalEntryAsync(reversal);

        // Mark the original as reversed
        await cosmo.PatchJournalEntryStatusAsync(id, companyId, JournalEntryStatus.Reversed);

        // Auto-post the reversal
        await PostJournalEntryAsync(created.id, companyId, createdBy);

        return created;
    }

    public async Task<JournalEntry?> GetJournalEntryAsync(string id, string companyId)
    {
        var entry = await cosmo.GetJournalEntryAsync(id, companyId);
        return entry != null ? Decrypt(entry) : null;
    }

    public async Task<List<JournalEntry>> GetJournalEntriesAsync(string companyId, JournalEntryStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var entries = await cosmo.GetJournalEntriesAsync(companyId, status, fromDate, toDate);
        return DecryptAll(entries);
    }

    public async Task<JournalEntry> UpdateDraftJournalEntryAsync(JournalEntry entry)
    {
        if (entry.Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException("Only draft journal entries can be edited.");

        ValidateJournalEntry(entry);

        for (int i = 0; i < entry.Lines.Count; i++)
            entry.Lines[i].LineNumber = i + 1;

        Encrypt(entry);
        return Decrypt(await cosmo.UpdateJournalEntryAsync(entry));
    }

    /// <summary>
    /// Generates a trial balance from posted journal entries.
    /// </summary>
    public async Task<List<TrialBalanceLine>> GetTrialBalanceAsync(string companyId, DateTime? asOfDate = null, bool cashBasis = false)
    {
        var entries = await cosmo.GetPostedJournalEntriesAsync(companyId, asOfDate, cashBasis ? true : null);
        DecryptAll(entries);
        var accounts = await cosmo.GetChartOfAccountsAsync(companyId, activeOnly: false);
        DecryptAll(accounts);

        var balances = new Dictionary<string, (decimal debit, decimal credit)>();

        foreach (var entry in entries)
        {
            foreach (var line in entry.Lines)
            {
                if (!balances.ContainsKey(line.AccountId))
                    balances[line.AccountId] = (0, 0);

                var current = balances[line.AccountId];
                balances[line.AccountId] = (current.debit + line.Debit, current.credit + line.Credit);
            }
        }

        return accounts
            .Where(a => balances.ContainsKey(a.id))
            .OrderBy(a => a.AccountNumber)
            .Select(a =>
            {
                var (debit, credit) = balances[a.id];
                return new TrialBalanceLine
                {
                    AccountId = a.id,
                    AccountNumber = a.AccountNumber,
                    AccountName = a.Name,
                    Category = a.Category,
                    Debit = debit,
                    Credit = credit
                };
            })
            .ToList();
    }

    /// <summary>
    /// Validates that a journal entry has balanced debits/credits and valid lines.
    /// </summary>
    public static void ValidateJournalEntry(JournalEntry entry)
    {
        if (entry.Lines == null || entry.Lines.Count < 2)
            throw new InvalidOperationException("A journal entry must have at least two lines.");

        if (entry.Lines.Any(l => l.Debit < 0 || l.Credit < 0))
            throw new InvalidOperationException("Debit and credit amounts must be non-negative.");

        if (entry.Lines.Any(l => l.Debit > 0 && l.Credit > 0))
            throw new InvalidOperationException("A line cannot have both a debit and a credit amount.");

        if (entry.Lines.Any(l => l.Debit == 0 && l.Credit == 0))
            throw new InvalidOperationException("Each line must have either a debit or credit amount.");

        if (string.IsNullOrEmpty(entry.CompanyId))
            throw new InvalidOperationException("Company ID is required.");

        if (entry.Lines.Any(l => string.IsNullOrEmpty(l.AccountId)))
            throw new InvalidOperationException("All lines must have an account specified.");

        var totalDebit = entry.Lines.Sum(l => l.Debit);
        var totalCredit = entry.Lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
            throw new InvalidOperationException($"Journal entry is not balanced. Total debits ({totalDebit:C}) must equal total credits ({totalCredit:C}).");
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
