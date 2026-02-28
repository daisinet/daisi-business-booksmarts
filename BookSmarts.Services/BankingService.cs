using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;
using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Going.Plaid.Transactions;
using Going.Plaid.Accounts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookSmarts.Services;

public class BankingService(
    BookSmartsCosmo cosmo,
    PlaidClient plaid,
    AccountingService accounting,
    EncryptionContext encryption,
    IConfiguration config,
    ILogger<BankingService> logger)
{
    private string? PlaidEncryptionKey => config["Plaid:EncryptionKey"];

    // ── Plaid Link Flow ──

    /// <summary>
    /// Creates a Plaid Link token for the client-side Link UI.
    /// </summary>
    public async Task<string> CreateLinkTokenAsync(string companyId, string userId)
    {
        var response = await plaid.LinkTokenCreateAsync(new LinkTokenCreateRequest
        {
            ClientName = "BookSmarts",
            Language = Language.English,
            CountryCodes = [CountryCode.Us],
            Products = [Products.Transactions],
            User = new LinkTokenCreateRequestUser { ClientUserId = userId },
            Webhook = config["Plaid:WebhookUrl"]
        });

        if (response.Error != null)
            throw new InvalidOperationException($"Plaid error: {response.Error.ErrorMessage}");

        return response.LinkToken;
    }

    /// <summary>
    /// Exchanges a public token from Plaid Link for an access token and creates a BankConnection.
    /// </summary>
    public async Task<BankConnection> ExchangePublicTokenAsync(string companyId, string publicToken, string institutionId, string institutionName)
    {
        var exchangeResponse = await plaid.ItemPublicTokenExchangeAsync(new ItemPublicTokenExchangeRequest
        {
            PublicToken = publicToken
        });

        if (exchangeResponse.Error != null)
            throw new InvalidOperationException($"Plaid error: {exchangeResponse.Error.ErrorMessage}");

        var accessToken = exchangeResponse.AccessToken;
        var itemId = exchangeResponse.ItemId;

        // Get accounts for this item
        var accountsResponse = await plaid.AccountsGetAsync(new AccountsGetRequest
        {
            AccessToken = accessToken
        });

        var bankAccounts = (accountsResponse.Accounts ?? []).Select(a => new BankAccount
        {
            PlaidAccountId = a.AccountId,
            Name = a.Name,
            OfficialName = a.OfficialName,
            Mask = a.Mask ?? "",
            PlaidType = a.Type.ToString(),
            PlaidSubType = a.Subtype?.ToString(),
            CurrentBalance = a.Balances?.Current,
            AvailableBalance = a.Balances?.Available,
            IsEnabled = true
        }).ToList();

        // Encrypt the access token: use ADK if encryption is enabled, otherwise server key
        string encryptedToken;
        if (encryption.IsUnlocked)
        {
            // Access token will be encrypted as part of the EncryptedPayload (BankConnection sensitive fields)
            // Store plaintext temporarily; FieldEncryption will handle it
            encryptedToken = accessToken;
        }
        else
        {
            var serverKey = PlaidEncryptionKey
                ?? throw new InvalidOperationException("Plaid:EncryptionKey is not configured.");
            encryptedToken = TokenEncryption.Encrypt(accessToken, serverKey);
        }

        var connection = new BankConnection
        {
            CompanyId = companyId,
            InstitutionId = institutionId,
            InstitutionName = institutionName,
            PlaidItemId = itemId,
            EncryptedAccessToken = encryptedToken,
            Status = BankConnectionStatus.Active,
            Accounts = bankAccounts
        };

        Encrypt(connection);
        return Decrypt(await cosmo.CreateBankConnectionAsync(connection));
    }

    // ── Account Mapping ──

    /// <summary>
    /// Maps a Plaid bank account to a COA account for journal entry creation.
    /// </summary>
    public async Task MapAccountAsync(string connectionId, string companyId, string plaidAccountId,
        string coaAccountId, string coaNumber, string coaName)
    {
        var connection = Decrypt(await cosmo.GetBankConnectionAsync(connectionId, companyId)
            ?? throw new InvalidOperationException("Bank connection not found."));

        var account = connection.Accounts.FirstOrDefault(a => a.PlaidAccountId == plaidAccountId)
            ?? throw new InvalidOperationException("Bank account not found on this connection.");

        account.MappedCoaAccountId = coaAccountId;
        account.MappedCoaAccountNumber = coaNumber;
        account.MappedCoaAccountName = coaName;

        Encrypt(connection);
        await cosmo.UpdateBankConnectionAsync(connection);
    }

    // ── Transaction Sync ──

    /// <summary>
    /// Syncs transactions for a bank connection using Plaid's cursor-based sync.
    /// </summary>
    public async Task SyncTransactionsAsync(string connectionId, string companyId)
    {
        var connection = Decrypt(await cosmo.GetBankConnectionAsync(connectionId, companyId)
            ?? throw new InvalidOperationException("Bank connection not found."));

        var accessToken = GetPlaidAccessToken(connection);
        var rules = await cosmo.GetRulesAsync(companyId, activeOnly: true);
        DecryptAll(rules);
        var cursor = connection.TransactionsCursor;
        var hasMore = true;

        while (hasMore)
        {
            var response = await plaid.TransactionsSyncAsync(new TransactionsSyncRequest
            {
                AccessToken = accessToken,
                Cursor = cursor,
                Count = 100
            });

            if (response.Error != null)
            {
                logger.LogWarning("Plaid sync error for connection {ConnectionId}: {Error}",
                    connectionId, response.Error.ErrorMessage);
                throw new InvalidOperationException($"Plaid sync error: {response.Error.ErrorMessage}");
            }

            // Process added transactions
            if (response.Added?.Count > 0)
            {
                var newTransactions = new List<BankTransaction>();
                foreach (var txn in response.Added)
                {
                    // Dedup check
                    var existing = await cosmo.GetTransactionByPlaidIdAsync(companyId, txn.TransactionId);
                    if (existing != null) continue;

                    var bankTxn = MapPlaidTransaction(txn, companyId, connectionId);

                    // Apply auto-categorization rules (suggestion mode)
                    var matchedRule = ApplyCategorizationRules(bankTxn, rules);
                    if (matchedRule != null)
                    {
                        bankTxn.MatchedRuleId = matchedRule.id;
                        bankTxn.CategorizedAccountId = matchedRule.TargetAccountId;
                        bankTxn.CategorizedAccountNumber = matchedRule.TargetAccountNumber;
                        bankTxn.CategorizedAccountName = matchedRule.TargetAccountName;
                        // Status stays Uncategorized — suggestion mode
                    }

                    Encrypt(bankTxn);
                    newTransactions.Add(bankTxn);
                }

                if (newTransactions.Count > 0)
                    await cosmo.BulkUpsertTransactionsAsync(newTransactions);
            }

            // Process modified transactions
            if (response.Modified?.Count > 0)
            {
                foreach (var txn in response.Modified)
                {
                    var existing = await cosmo.GetTransactionByPlaidIdAsync(companyId, txn.TransactionId);
                    if (existing == null) continue;

                    Decrypt(existing);

                    // Update mutable fields but preserve categorization
                    existing.Amount = txn.Amount ?? existing.Amount;
                    existing.Name = txn.OriginalDescription ?? txn.MerchantName ?? existing.Name;
                    existing.MerchantName = txn.MerchantName;
                    existing.IsPending = txn.Pending ?? existing.IsPending;
                    existing.TransactionDate = txn.Date?.ToDateTime(TimeOnly.MinValue) ?? existing.TransactionDate;
                    existing.AuthorizedDate = txn.AuthorizedDate?.ToDateTime(TimeOnly.MinValue);

                    Encrypt(existing);
                    await cosmo.UpdateTransactionAsync(existing);
                }
            }

            // Process removed transactions
            if (response.Removed?.Count > 0)
            {
                foreach (var removed in response.Removed)
                {
                    var existing = await cosmo.GetTransactionByPlaidIdAsync(companyId, removed.TransactionId);
                    if (existing != null)
                        await cosmo.DeleteTransactionAsync(existing.id, companyId);
                }
            }

            cursor = response.NextCursor;
            hasMore = response.HasMore;
        }

        // Save cursor for next sync
        await cosmo.PatchBankConnectionCursorAsync(connectionId, companyId, cursor);
    }

    // ── Categorization ──

    /// <summary>
    /// Categorizes a transaction: creates a balanced journal entry and marks the transaction.
    /// Plaid convention: positive = money out (expense), negative = money in (income).
    /// </summary>
    public async Task<BankTransaction> CategorizeTransactionAsync(
        string txnId, string companyId,
        string targetAccountId, string targetAccountNumber, string targetAccountName,
        bool createRule = false)
    {
        var txn = Decrypt(await cosmo.GetTransactionAsync(txnId, companyId)
            ?? throw new InvalidOperationException("Transaction not found."));

        if (txn.Status == BankTransactionStatus.Categorized)
            throw new InvalidOperationException("Transaction is already categorized.");

        // Find the bank account's mapped COA account
        var connection = Decrypt(await cosmo.GetBankConnectionAsync(txn.BankConnectionId, companyId)
            ?? throw new InvalidOperationException("Bank connection not found."));

        var bankAccount = connection.Accounts.FirstOrDefault(a => a.PlaidAccountId == txn.PlaidAccountId);
        if (bankAccount?.MappedCoaAccountId == null)
            throw new InvalidOperationException("Bank account is not mapped to a COA account. Please map it first.");

        var absAmount = Math.Abs(txn.Amount);
        var description = txn.MerchantName ?? txn.Name;

        // Plaid: positive = money leaving (expense), negative = money entering (income)
        var journalEntry = new JournalEntry
        {
            CompanyId = companyId,
            EntryDate = txn.TransactionDate,
            Description = description,
            SourceType = SourceType.BankImport,
            SourceId = txn.id,
            Lines = txn.Amount > 0
                ? new List<JournalLine>
                {
                    // Money out: debit expense, credit bank
                    new() { AccountId = targetAccountId, AccountNumber = targetAccountNumber, AccountName = targetAccountName, Debit = absAmount, Credit = 0, Description = description },
                    new() { AccountId = bankAccount.MappedCoaAccountId, AccountNumber = bankAccount.MappedCoaAccountNumber ?? "", AccountName = bankAccount.MappedCoaAccountName ?? "", Debit = 0, Credit = absAmount, Description = description }
                }
                : new List<JournalLine>
                {
                    // Money in: debit bank, credit revenue
                    new() { AccountId = bankAccount.MappedCoaAccountId, AccountNumber = bankAccount.MappedCoaAccountNumber ?? "", AccountName = bankAccount.MappedCoaAccountName ?? "", Debit = absAmount, Credit = 0, Description = description },
                    new() { AccountId = targetAccountId, AccountNumber = targetAccountNumber, AccountName = targetAccountName, Debit = 0, Credit = absAmount, Description = description }
                }
        };

        // Create and auto-post the journal entry
        var created = await accounting.CreateJournalEntryAsync(journalEntry);
        await accounting.PostJournalEntryAsync(created.id, companyId, "BankImport");

        // Update transaction status — if encryption is enabled, do full read-modify-write
        // because CategorizedAccountName is an encrypted field
        if (encryption.IsUnlocked)
        {
            txn.Status = BankTransactionStatus.Categorized;
            txn.CategorizedAccountId = targetAccountId;
            txn.CategorizedAccountNumber = targetAccountNumber;
            txn.CategorizedAccountName = targetAccountName;
            txn.JournalEntryId = created.id;
            Encrypt(txn);
            await cosmo.UpdateTransactionAsync(txn);
        }
        else
        {
            await cosmo.PatchTransactionStatusAsync(txn.id, companyId,
                BankTransactionStatus.Categorized,
                targetAccountId, targetAccountNumber, targetAccountName,
                created.id);
            txn.Status = BankTransactionStatus.Categorized;
            txn.CategorizedAccountId = targetAccountId;
            txn.CategorizedAccountNumber = targetAccountNumber;
            txn.CategorizedAccountName = targetAccountName;
            txn.JournalEntryId = created.id;
        }

        // Optionally create a categorization rule
        if (createRule && !string.IsNullOrEmpty(txn.MerchantName))
        {
            var rule = new CategorizationRule
            {
                CompanyId = companyId,
                MerchantNameContains = txn.MerchantName,
                PlaidCategory = txn.PlaidCategories.Count > 0 ? txn.PlaidCategories[0] : null,
                TargetAccountId = targetAccountId,
                TargetAccountNumber = targetAccountNumber,
                TargetAccountName = targetAccountName,
                Priority = 100,
                IsActive = true,
                TimesApplied = 1
            };
            Encrypt(rule);
            await cosmo.CreateRuleAsync(rule);
        }

        return txn;
    }

    /// <summary>
    /// Categorizes multiple transactions in bulk with the same target account.
    /// </summary>
    public async Task<int> BulkCategorizeAsync(
        List<string> txnIds, string companyId,
        string targetAccountId, string targetAccountNumber, string targetAccountName)
    {
        var count = 0;
        foreach (var txnId in txnIds)
        {
            try
            {
                await CategorizeTransactionAsync(txnId, companyId, targetAccountId, targetAccountNumber, targetAccountName);
                count++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to categorize transaction {TxnId}", txnId);
            }
        }
        return count;
    }

    /// <summary>
    /// Excludes a transaction from categorization.
    /// </summary>
    public async Task ExcludeTransactionAsync(string txnId, string companyId)
    {
        await cosmo.PatchTransactionStatusAsync(txnId, companyId, BankTransactionStatus.Excluded);
    }

    /// <summary>
    /// Uncategorizes a transaction and voids its journal entry.
    /// </summary>
    public async Task UncategorizeTransactionAsync(string txnId, string companyId)
    {
        var txn = Decrypt(await cosmo.GetTransactionAsync(txnId, companyId)
            ?? throw new InvalidOperationException("Transaction not found."));

        if (txn.Status != BankTransactionStatus.Categorized)
            throw new InvalidOperationException("Transaction is not categorized.");

        // Void the associated journal entry
        if (!string.IsNullOrEmpty(txn.JournalEntryId))
        {
            try
            {
                await accounting.VoidJournalEntryAsync(txn.JournalEntryId, companyId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to void journal entry {JeId} for transaction {TxnId}",
                    txn.JournalEntryId, txnId);
            }
        }

        // Reset transaction to uncategorized
        txn.Status = BankTransactionStatus.Uncategorized;
        txn.CategorizedAccountId = null;
        txn.CategorizedAccountNumber = null;
        txn.CategorizedAccountName = null;
        txn.JournalEntryId = null;
        Encrypt(txn);
        await cosmo.UpdateTransactionAsync(txn);
    }

    // ── Disconnect ──

    /// <summary>
    /// Disconnects a bank connection by removing the Plaid item.
    /// </summary>
    public async Task DisconnectAsync(string connectionId, string companyId)
    {
        var connection = Decrypt(await cosmo.GetBankConnectionAsync(connectionId, companyId)
            ?? throw new InvalidOperationException("Bank connection not found."));

        try
        {
            var accessToken = GetPlaidAccessToken(connection);
            await plaid.ItemRemoveAsync(new ItemRemoveRequest { AccessToken = accessToken });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to remove Plaid item for connection {ConnectionId}", connectionId);
        }

        await cosmo.PatchBankConnectionStatusAsync(connectionId, companyId, BankConnectionStatus.Disconnected);
    }

    // ── Read Passthroughs ──

    public async Task<List<BankConnection>> GetConnectionsAsync(string companyId)
    {
        var connections = await cosmo.GetBankConnectionsAsync(companyId);
        return DecryptAll(connections);
    }

    public async Task<BankConnection?> GetConnectionAsync(string connectionId, string companyId)
    {
        var connection = await cosmo.GetBankConnectionAsync(connectionId, companyId);
        return connection != null ? Decrypt(connection) : null;
    }

    public async Task<List<BankTransaction>> GetTransactionsAsync(string companyId,
        BankTransactionStatus? status = null, string? bankConnectionId = null,
        string? plaidAccountId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var txns = await cosmo.GetTransactionsAsync(companyId, status, bankConnectionId, plaidAccountId, fromDate, toDate);
        return DecryptAll(txns);
    }

    public async Task<BankTransaction?> GetTransactionAsync(string txnId, string companyId)
    {
        var txn = await cosmo.GetTransactionAsync(txnId, companyId);
        return txn != null ? Decrypt(txn) : null;
    }

    public Task<int> GetUncategorizedCountAsync(string companyId)
        => cosmo.GetUncategorizedCountAsync(companyId);

    /// <summary>
    /// Finds uncategorized transactions matching by merchant name and/or Plaid category.
    /// Returns merchant matches and category-only matches separately for UI display.
    /// </summary>
    public async Task<RulePreviewResult> FindMatchingUncategorizedAsync(string companyId, string? merchantContains, List<string>? plaidCategories, string? excludeTxnId = null)
    {
        var all = await cosmo.GetTransactionsAsync(companyId, BankTransactionStatus.Uncategorized);
        DecryptAll(all);
        var merchantMatches = new List<BankTransaction>();
        var categoryMatches = new List<BankTransaction>();

        var primaryCategory = plaidCategories?.FirstOrDefault();

        foreach (var t in all)
        {
            if (t.id == excludeTxnId) continue;

            // Merchant name match
            if (!string.IsNullOrEmpty(merchantContains))
            {
                var name = (t.MerchantName ?? t.Name) ?? "";
                if (name.Contains(merchantContains, StringComparison.OrdinalIgnoreCase))
                {
                    merchantMatches.Add(t);
                    continue;
                }
            }

            // Plaid category match (only if not already matched by merchant)
            if (!string.IsNullOrEmpty(primaryCategory) && t.PlaidCategories.Count > 0)
            {
                if (t.PlaidCategories.Any(c => c.Equals(primaryCategory, StringComparison.OrdinalIgnoreCase)))
                {
                    categoryMatches.Add(t);
                }
            }
        }

        return new RulePreviewResult { MerchantMatches = merchantMatches, CategoryMatches = categoryMatches };
    }

    public class RulePreviewResult
    {
        public List<BankTransaction> MerchantMatches { get; set; } = new();
        public List<BankTransaction> CategoryMatches { get; set; } = new();
        public int TotalCount => MerchantMatches.Count + CategoryMatches.Count;
    }

    public async Task<List<CategorizationRule>> GetRulesAsync(string companyId, bool activeOnly = false)
    {
        var rules = await cosmo.GetRulesAsync(companyId, activeOnly);
        return DecryptAll(rules);
    }

    public async Task<CategorizationRule?> GetRuleAsync(string ruleId, string companyId)
    {
        var rule = await cosmo.GetRuleAsync(ruleId, companyId);
        return rule != null ? Decrypt(rule) : null;
    }

    public async Task<CategorizationRule> CreateRuleAsync(CategorizationRule rule)
    {
        Encrypt(rule);
        return Decrypt(await cosmo.CreateRuleAsync(rule));
    }

    public async Task<CategorizationRule> UpdateRuleAsync(CategorizationRule rule)
    {
        Encrypt(rule);
        return Decrypt(await cosmo.UpdateRuleAsync(rule));
    }

    public Task DeleteRuleAsync(string ruleId, string companyId)
        => cosmo.DeleteRuleAsync(ruleId, companyId);

    // ── Internal Helpers ──

    /// <summary>
    /// Gets the plaintext Plaid access token from a BankConnection.
    /// If encryption is enabled, the token is part of the decrypted fields.
    /// If not, decrypts with the server-side Plaid:EncryptionKey.
    /// </summary>
    private string GetPlaidAccessToken(BankConnection connection)
    {
        if (encryption.IsUnlocked)
        {
            // After DecryptFields, EncryptedAccessToken contains the plaintext token
            return connection.EncryptedAccessToken;
        }

        var serverKey = PlaidEncryptionKey
            ?? throw new InvalidOperationException("Plaid:EncryptionKey is not configured.");
        return TokenEncryption.Decrypt(connection.EncryptedAccessToken, serverKey);
    }

    private static BankTransaction MapPlaidTransaction(Transaction txn, string companyId, string connectionId)
    {
        var categories = new List<string>();
        if (txn.PersonalFinanceCategory != null)
        {
            categories.Add(txn.PersonalFinanceCategory.Primary.ToString());
            if (!string.IsNullOrEmpty(txn.PersonalFinanceCategory.Detailed?.ToString()))
                categories.Add(txn.PersonalFinanceCategory.Detailed.ToString()!);
        }

        return new BankTransaction
        {
            CompanyId = companyId,
            BankConnectionId = connectionId,
            PlaidAccountId = txn.AccountId,
            PlaidTransactionId = txn.TransactionId,
            TransactionDate = txn.Date?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow,
            AuthorizedDate = txn.AuthorizedDate?.ToDateTime(TimeOnly.MinValue),
            MerchantName = txn.MerchantName,
            Name = txn.OriginalDescription ?? txn.MerchantName ?? "",
            Amount = txn.Amount ?? 0m,
            IsoCurrencyCode = txn.IsoCurrencyCode,
            PlaidCategories = categories,
            IsPending = txn.Pending ?? false,
            Status = BankTransactionStatus.Uncategorized
        };
    }

    /// <summary>
    /// Applies categorization rules to a transaction. First match by priority wins.
    /// Returns the matched rule or null.
    /// </summary>
    internal static CategorizationRule? ApplyCategorizationRules(BankTransaction txn, List<CategorizationRule> rules)
    {
        if (txn.Status != BankTransactionStatus.Uncategorized)
            return null;

        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            if (!rule.IsActive) continue;

            // Merchant name contains match (case-insensitive)
            if (!string.IsNullOrEmpty(rule.MerchantNameContains))
            {
                var merchantName = txn.MerchantName ?? txn.Name;
                if (!string.IsNullOrEmpty(merchantName) &&
                    merchantName.Contains(rule.MerchantNameContains, StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }

            // Plaid category match
            if (!string.IsNullOrEmpty(rule.PlaidCategory) && txn.PlaidCategories.Count > 0)
            {
                if (txn.PlaidCategories.Any(c =>
                    c.Contains(rule.PlaidCategory, StringComparison.OrdinalIgnoreCase)))
                {
                    return rule;
                }
            }
        }

        return null;
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
