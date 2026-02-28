using BookSmarts.Core.Encryption;
using BookSmarts.Core.Models;
using BookSmarts.Data;
using Microsoft.Extensions.Configuration;

namespace BookSmarts.Services;

/// <summary>
/// One-time migration to encrypt or decrypt all existing documents for an account.
/// </summary>
public class EncryptionMigrationService(
    BookSmartsCosmo cosmo,
    EncryptionContext encryption,
    IConfiguration config)
{
    /// <summary>
    /// Encrypts all existing documents for an account. Idempotent — skips already-encrypted docs.
    /// </summary>
    public async Task EncryptAllDataAsync(string accountId, IProgress<MigrationProgress>? progress = null)
    {
        var adk = encryption.GetAdk();
        var total = 0;
        var processed = 0;

        // Organizations container: Organizations, Companies, Divisions
        var orgs = await cosmo.GetOrganizationsAsync(accountId);
        var companies = await cosmo.GetCompaniesAsync(accountId);
        var allDivisions = new List<Division>();
        foreach (var org in orgs)
        {
            var divs = await cosmo.GetDivisionsAsync(accountId, org.id);
            allDivisions.AddRange(divs);
        }

        total += orgs.Count + companies.Count + allDivisions.Count;

        foreach (var org in orgs)
        {
            if (org.EncryptedPayload != null) { processed++; continue; }
            FieldEncryption.EncryptFields(org, adk);
            await cosmo.UpdateOrganizationAsync(org);
            processed++;
            progress?.Report(new MigrationProgress(processed, total, "Organizations"));
        }

        foreach (var company in companies)
        {
            if (company.EncryptedPayload != null) { processed++; continue; }
            FieldEncryption.EncryptFields(company, adk);
            await cosmo.UpdateCompanyAsync(company);
            processed++;
            progress?.Report(new MigrationProgress(processed, total, "Companies"));
        }

        foreach (var div in allDivisions)
        {
            if (div.EncryptedPayload != null) { processed++; continue; }
            FieldEncryption.EncryptFields(div, adk);
            await cosmo.UpdateDivisionAsync(div);
            processed++;
            progress?.Report(new MigrationProgress(processed, total, "Divisions"));
        }

        // Per-company containers
        foreach (var company in companies)
        {
            var companyId = company.id;

            // Chart of accounts
            var accounts = await cosmo.GetChartOfAccountsAsync(companyId, activeOnly: false);
            total += accounts.Count;
            foreach (var entry in accounts)
            {
                if (entry.EncryptedPayload != null) { processed++; continue; }
                FieldEncryption.EncryptFields(entry, adk);
                await cosmo.UpdateAccountEntryAsync(entry);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Chart of Accounts"));
            }

            // Journal entries
            var journals = await cosmo.GetJournalEntriesAsync(companyId);
            total += journals.Count;
            foreach (var je in journals)
            {
                if (je.EncryptedPayload != null) { processed++; continue; }
                FieldEncryption.EncryptFields(je, adk);
                await cosmo.UpdateJournalEntryAsync(je);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Journal Entries"));
            }

            // Fiscal years
            var fiscalYears = await cosmo.GetFiscalYearsAsync(companyId);
            total += fiscalYears.Count;
            foreach (var fy in fiscalYears)
            {
                if (fy.EncryptedPayload != null) { processed++; continue; }
                FieldEncryption.EncryptFields(fy, adk);
                await cosmo.UpdateFiscalYearAsync(fy);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Fiscal Years"));
            }

            // Bank connections (special handling: migrate Plaid tokens)
            var connections = await cosmo.GetBankConnectionsAsync(companyId);
            total += connections.Count;
            foreach (var bc in connections)
            {
                if (bc.EncryptedPayload != null) { processed++; continue; }

                // Decrypt the access token from old server-side encryption
                var serverKey = config["Plaid:EncryptionKey"];
                if (!string.IsNullOrEmpty(serverKey) && !string.IsNullOrEmpty(bc.EncryptedAccessToken))
                {
                    try
                    {
                        bc.EncryptedAccessToken = TokenEncryption.Decrypt(bc.EncryptedAccessToken, serverKey);
                    }
                    catch
                    {
                        // Already plaintext or different encryption — leave as-is
                    }
                }

                FieldEncryption.EncryptFields(bc, adk);
                await cosmo.UpdateBankConnectionAsync(bc);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Bank Connections"));
            }

            // Bank transactions
            var transactions = await cosmo.GetTransactionsAsync(companyId, maxItems: 10000);
            total += transactions.Count;
            foreach (var txn in transactions)
            {
                if (txn.EncryptedPayload != null) { processed++; continue; }
                FieldEncryption.EncryptFields(txn, adk);
                await cosmo.UpdateTransactionAsync(txn);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Bank Transactions"));
            }

            // Categorization rules
            var rules = await cosmo.GetRulesAsync(companyId);
            total += rules.Count;
            foreach (var rule in rules)
            {
                if (rule.EncryptedPayload != null) { processed++; continue; }
                FieldEncryption.EncryptFields(rule, adk);
                await cosmo.UpdateRuleAsync(rule);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Categorization Rules"));
            }
        }

        progress?.Report(new MigrationProgress(processed, total, "Complete"));
    }

    /// <summary>
    /// Decrypts all existing documents for an account (used when disabling encryption).
    /// </summary>
    public async Task DecryptAllDataAsync(string accountId, IProgress<MigrationProgress>? progress = null)
    {
        var adk = encryption.GetAdk();
        var total = 0;
        var processed = 0;

        var orgs = await cosmo.GetOrganizationsAsync(accountId);
        var companies = await cosmo.GetCompaniesAsync(accountId);
        var allDivisions = new List<Division>();
        foreach (var org in orgs)
        {
            var divs = await cosmo.GetDivisionsAsync(accountId, org.id);
            allDivisions.AddRange(divs);
        }

        total += orgs.Count + companies.Count + allDivisions.Count;

        foreach (var org in orgs)
        {
            if (org.EncryptedPayload == null) { processed++; continue; }
            FieldEncryption.DecryptFields(org, adk);
            await cosmo.UpdateOrganizationAsync(org);
            processed++;
            progress?.Report(new MigrationProgress(processed, total, "Organizations"));
        }

        foreach (var company in companies)
        {
            if (company.EncryptedPayload == null) { processed++; continue; }
            FieldEncryption.DecryptFields(company, adk);
            await cosmo.UpdateCompanyAsync(company);
            processed++;
            progress?.Report(new MigrationProgress(processed, total, "Companies"));
        }

        foreach (var div in allDivisions)
        {
            if (div.EncryptedPayload == null) { processed++; continue; }
            FieldEncryption.DecryptFields(div, adk);
            await cosmo.UpdateDivisionAsync(div);
            processed++;
            progress?.Report(new MigrationProgress(processed, total, "Divisions"));
        }

        foreach (var company in companies)
        {
            var companyId = company.id;

            var accounts = await cosmo.GetChartOfAccountsAsync(companyId, activeOnly: false);
            total += accounts.Count;
            foreach (var entry in accounts)
            {
                if (entry.EncryptedPayload == null) { processed++; continue; }
                FieldEncryption.DecryptFields(entry, adk);
                await cosmo.UpdateAccountEntryAsync(entry);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Chart of Accounts"));
            }

            var journals = await cosmo.GetJournalEntriesAsync(companyId);
            total += journals.Count;
            foreach (var je in journals)
            {
                if (je.EncryptedPayload == null) { processed++; continue; }
                FieldEncryption.DecryptFields(je, adk);
                await cosmo.UpdateJournalEntryAsync(je);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Journal Entries"));
            }

            var fiscalYears = await cosmo.GetFiscalYearsAsync(companyId);
            total += fiscalYears.Count;
            foreach (var fy in fiscalYears)
            {
                if (fy.EncryptedPayload == null) { processed++; continue; }
                FieldEncryption.DecryptFields(fy, adk);
                await cosmo.UpdateFiscalYearAsync(fy);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Fiscal Years"));
            }

            // Bank connections: restore server-side token encryption
            var connections = await cosmo.GetBankConnectionsAsync(companyId);
            total += connections.Count;
            foreach (var bc in connections)
            {
                if (bc.EncryptedPayload == null) { processed++; continue; }
                FieldEncryption.DecryptFields(bc, adk);

                // Re-encrypt access token with server key
                var serverKey = config["Plaid:EncryptionKey"];
                if (!string.IsNullOrEmpty(serverKey) && !string.IsNullOrEmpty(bc.EncryptedAccessToken))
                {
                    bc.EncryptedAccessToken = TokenEncryption.Encrypt(bc.EncryptedAccessToken, serverKey);
                }

                await cosmo.UpdateBankConnectionAsync(bc);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Bank Connections"));
            }

            var transactions = await cosmo.GetTransactionsAsync(companyId, maxItems: 10000);
            total += transactions.Count;
            foreach (var txn in transactions)
            {
                if (txn.EncryptedPayload == null) { processed++; continue; }
                FieldEncryption.DecryptFields(txn, adk);
                await cosmo.UpdateTransactionAsync(txn);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Bank Transactions"));
            }

            var rules = await cosmo.GetRulesAsync(companyId);
            total += rules.Count;
            foreach (var rule in rules)
            {
                if (rule.EncryptedPayload == null) { processed++; continue; }
                FieldEncryption.DecryptFields(rule, adk);
                await cosmo.UpdateRuleAsync(rule);
                processed++;
                progress?.Report(new MigrationProgress(processed, total, "Categorization Rules"));
            }
        }

        progress?.Report(new MigrationProgress(processed, total, "Complete"));
    }
}

public record MigrationProgress(int Processed, int Total, string CurrentPhase)
{
    public double Percentage => Total > 0 ? (double)Processed / Total * 100 : 0;
}
