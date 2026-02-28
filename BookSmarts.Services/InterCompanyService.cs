using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class InterCompanyService(
    BookSmartsCosmo cosmo,
    AccountingService accounting,
    ChartOfAccountsService chartOfAccounts,
    OrganizationService organizations,
    EncryptionContext encryption)
{
    /// <summary>
    /// Creates an inter-company transaction with journal entries in both companies.
    /// </summary>
    public async Task<InterCompanyTransaction> CreateInterCompanyTransactionAsync(InterCompanyTransactionRequest request)
    {
        ValidateRequest(request);

        // Get company names
        var sourceCompany = await organizations.GetCompanyAsync(request.SourceCompanyId, request.AccountId)
            ?? throw new InvalidOperationException("Source company not found.");
        var targetCompany = await organizations.GetCompanyAsync(request.TargetCompanyId, request.AccountId)
            ?? throw new InvalidOperationException("Target company not found.");

        // Get IC account details
        var sourceIcAccount = await chartOfAccounts.GetAccountEntryAsync(request.SourceIcAccountId, request.SourceCompanyId)
            ?? throw new InvalidOperationException("Source IC account not found.");
        var targetIcAccount = await chartOfAccounts.GetAccountEntryAsync(request.TargetIcAccountId, request.TargetCompanyId)
            ?? throw new InvalidOperationException("Target IC account not found.");

        // Get source/target revenue/expense account details
        var sourceAccount = await chartOfAccounts.GetAccountEntryAsync(request.SourceAccountId, request.SourceCompanyId)
            ?? throw new InvalidOperationException("Source account not found.");
        var targetAccount = await chartOfAccounts.GetAccountEntryAsync(request.TargetAccountId, request.TargetCompanyId)
            ?? throw new InvalidOperationException("Target account not found.");

        // Create JE in source company: DR IC Receivable / CR source account
        var sourceJe = new JournalEntry
        {
            CompanyId = request.SourceCompanyId,
            EntryDate = request.TransactionDate,
            Description = $"IC: {request.Description}",
            SourceType = SourceType.InterCompany,
            Lines = new()
            {
                new JournalLine
                {
                    AccountId = request.SourceIcAccountId,
                    AccountNumber = sourceIcAccount.AccountNumber,
                    AccountName = sourceIcAccount.Name,
                    Debit = request.Amount,
                    Credit = 0
                },
                new JournalLine
                {
                    AccountId = request.SourceAccountId,
                    AccountNumber = sourceAccount.AccountNumber,
                    AccountName = sourceAccount.Name,
                    Debit = 0,
                    Credit = request.Amount
                }
            }
        };

        var createdSourceJe = await accounting.CreateJournalEntryAsync(sourceJe);
        await accounting.PostJournalEntryAsync(createdSourceJe.id, request.SourceCompanyId);

        // Create JE in target company: DR target account / CR IC Payable
        var targetJe = new JournalEntry
        {
            CompanyId = request.TargetCompanyId,
            EntryDate = request.TransactionDate,
            Description = $"IC: {request.Description}",
            SourceType = SourceType.InterCompany,
            Lines = new()
            {
                new JournalLine
                {
                    AccountId = request.TargetAccountId,
                    AccountNumber = targetAccount.AccountNumber,
                    AccountName = targetAccount.Name,
                    Debit = request.Amount,
                    Credit = 0
                },
                new JournalLine
                {
                    AccountId = request.TargetIcAccountId,
                    AccountNumber = targetIcAccount.AccountNumber,
                    AccountName = targetIcAccount.Name,
                    Debit = 0,
                    Credit = request.Amount
                }
            }
        };

        var createdTargetJe = await accounting.CreateJournalEntryAsync(targetJe);
        await accounting.PostJournalEntryAsync(createdTargetJe.id, request.TargetCompanyId);

        // Create IC transaction record
        var icTransaction = new InterCompanyTransaction
        {
            OrganizationId = request.OrganizationId,
            AccountId = request.AccountId,
            SourceCompanyId = request.SourceCompanyId,
            SourceCompanyName = sourceCompany.Name,
            TargetCompanyId = request.TargetCompanyId,
            TargetCompanyName = targetCompany.Name,
            SourceJournalEntryId = createdSourceJe.id,
            TargetJournalEntryId = createdTargetJe.id,
            Amount = request.Amount,
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            SourceAccountId = request.SourceAccountId,
            SourceAccountNumber = sourceAccount.AccountNumber,
            TargetAccountId = request.TargetAccountId,
            TargetAccountNumber = targetAccount.AccountNumber,
            SourceIcAccountId = request.SourceIcAccountId,
            TargetIcAccountId = request.TargetIcAccountId
        };

        Encrypt(icTransaction);
        return Decrypt(await cosmo.CreateInterCompanyTransactionAsync(icTransaction));
    }

    /// <summary>
    /// Voids an inter-company transaction and both of its journal entries.
    /// </summary>
    public async Task<InterCompanyTransaction> VoidInterCompanyTransactionAsync(string id, string organizationId)
    {
        var ic = Decrypt(await cosmo.GetInterCompanyTransactionAsync(id, organizationId)
            ?? throw new InvalidOperationException("Inter-company transaction not found."));

        if (ic.Status != InterCompanyStatus.Posted)
            throw new InvalidOperationException("Only posted inter-company transactions can be voided.");

        // Void both journal entries
        await accounting.VoidJournalEntryAsync(ic.SourceJournalEntryId, ic.SourceCompanyId);
        await accounting.VoidJournalEntryAsync(ic.TargetJournalEntryId, ic.TargetCompanyId);

        ic.Status = InterCompanyStatus.Voided;
        Encrypt(ic);
        return Decrypt(await cosmo.UpdateInterCompanyTransactionAsync(ic));
    }

    public async Task<InterCompanyTransaction?> GetInterCompanyTransactionAsync(string id, string organizationId)
    {
        var ic = await cosmo.GetInterCompanyTransactionAsync(id, organizationId);
        return ic != null ? Decrypt(ic) : null;
    }

    public async Task<List<InterCompanyTransaction>> GetInterCompanyTransactionsAsync(
        string organizationId, InterCompanyStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var results = await cosmo.GetInterCompanyTransactionsAsync(organizationId, status, fromDate, toDate);
        return DecryptAll(results);
    }

    public async Task<List<InterCompanyTransaction>> GetPostedInterCompanyTransactionsAsync(string organizationId, DateTime? asOfDate = null)
    {
        var results = await cosmo.GetPostedInterCompanyTransactionsAsync(organizationId, asOfDate);
        return DecryptAll(results);
    }

    /// <summary>
    /// Validates an inter-company transaction request.
    /// </summary>
    public static void ValidateRequest(InterCompanyTransactionRequest request)
    {
        if (string.IsNullOrEmpty(request.OrganizationId))
            throw new InvalidOperationException("Organization ID is required.");

        if (string.IsNullOrEmpty(request.AccountId))
            throw new InvalidOperationException("Account ID is required.");

        if (string.IsNullOrEmpty(request.SourceCompanyId))
            throw new InvalidOperationException("Source company is required.");

        if (string.IsNullOrEmpty(request.TargetCompanyId))
            throw new InvalidOperationException("Target company is required.");

        if (request.SourceCompanyId == request.TargetCompanyId)
            throw new InvalidOperationException("Source and target companies must be different.");

        if (request.Amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");

        if (string.IsNullOrEmpty(request.SourceAccountId))
            throw new InvalidOperationException("Source account is required.");

        if (string.IsNullOrEmpty(request.TargetAccountId))
            throw new InvalidOperationException("Target account is required.");

        if (string.IsNullOrEmpty(request.SourceIcAccountId))
            throw new InvalidOperationException("Source IC account is required.");

        if (string.IsNullOrEmpty(request.TargetIcAccountId))
            throw new InvalidOperationException("Target IC account is required.");
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
