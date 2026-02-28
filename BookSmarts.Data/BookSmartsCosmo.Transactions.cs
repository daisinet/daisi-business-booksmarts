using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string TransactionIdPrefix = "bt";
    public const string TransactionsContainerName = "Transactions";
    public const string TransactionsPartitionKeyName = "CompanyId";

    public PartitionKey GetTransactionPartitionKey(string companyId) => new(companyId);

    public virtual async Task<BankTransaction> CreateTransactionAsync(BankTransaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.id))
            transaction.id = GenerateId(TransactionIdPrefix);
        transaction.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(TransactionsContainerName);
        var response = await container.CreateItemAsync(transaction, GetTransactionPartitionKey(transaction.CompanyId));
        return response.Resource;
    }

    public virtual async Task<BankTransaction?> GetTransactionAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(TransactionsContainerName);
            var response = await container.ReadItemAsync<BankTransaction>(id, GetTransactionPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<BankTransaction>> GetTransactionsAsync(
        string companyId,
        BankTransactionStatus? status = null,
        string? bankConnectionId = null,
        string? plaidAccountId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int maxItems = 200)
    {
        var container = await GetContainerAsync(TransactionsContainerName);
        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'BankTransaction'";

        if (status.HasValue)
            sql += " AND c.Status = @status";
        if (!string.IsNullOrEmpty(bankConnectionId))
            sql += " AND c.BankConnectionId = @connectionId";
        if (!string.IsNullOrEmpty(plaidAccountId))
            sql += " AND c.PlaidAccountId = @accountId";
        if (fromDate.HasValue)
            sql += " AND c.TransactionDate >= @fromDate";
        if (toDate.HasValue)
            sql += " AND c.TransactionDate <= @toDate";
        sql += " ORDER BY c.TransactionDate DESC";

        var query = new QueryDefinition(sql)
            .WithParameter("@companyId", companyId);

        if (status.HasValue)
            query = query.WithParameter("@status", (int)status.Value);
        if (!string.IsNullOrEmpty(bankConnectionId))
            query = query.WithParameter("@connectionId", bankConnectionId);
        if (!string.IsNullOrEmpty(plaidAccountId))
            query = query.WithParameter("@accountId", plaidAccountId);
        if (fromDate.HasValue)
            query = query.WithParameter("@fromDate", fromDate.Value);
        if (toDate.HasValue)
            query = query.WithParameter("@toDate", toDate.Value);

        var results = new List<BankTransaction>();
        using var iterator = container.GetItemQueryIterator<BankTransaction>(query,
            requestOptions: new QueryRequestOptions { MaxItemCount = maxItems });
        while (iterator.HasMoreResults && results.Count < maxItems)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results.Take(maxItems).ToList();
    }

    public virtual async Task<BankTransaction?> GetTransactionByPlaidIdAsync(string companyId, string plaidTransactionId)
    {
        var container = await GetContainerAsync(TransactionsContainerName);
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'BankTransaction' AND c.PlaidTransactionId = @plaidId")
            .WithParameter("@companyId", companyId)
            .WithParameter("@plaidId", plaidTransactionId);

        using var iterator = container.GetItemQueryIterator<BankTransaction>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public virtual async Task<BankTransaction> UpdateTransactionAsync(BankTransaction transaction)
    {
        transaction.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(TransactionsContainerName);
        var response = await container.UpsertItemAsync(transaction, GetTransactionPartitionKey(transaction.CompanyId));
        return response.Resource;
    }

    public virtual async Task PatchTransactionStatusAsync(string id, string companyId, BankTransactionStatus status,
        string? categorizedAccountId = null, string? categorizedAccountNumber = null, string? categorizedAccountName = null,
        string? journalEntryId = null, string? matchedRuleId = null)
    {
        var container = await GetContainerAsync(TransactionsContainerName);
        var operations = new List<PatchOperation>
        {
            PatchOperation.Set("/Status", (int)status),
            PatchOperation.Set("/UpdatedUtc", DateTime.UtcNow)
        };

        if (categorizedAccountId != null)
            operations.Add(PatchOperation.Set("/CategorizedAccountId", categorizedAccountId));
        if (categorizedAccountNumber != null)
            operations.Add(PatchOperation.Set("/CategorizedAccountNumber", categorizedAccountNumber));
        if (categorizedAccountName != null)
            operations.Add(PatchOperation.Set("/CategorizedAccountName", categorizedAccountName));
        if (journalEntryId != null)
            operations.Add(PatchOperation.Set("/JournalEntryId", journalEntryId));
        if (matchedRuleId != null)
            operations.Add(PatchOperation.Set("/MatchedRuleId", matchedRuleId));

        await container.PatchItemAsync<BankTransaction>(id, GetTransactionPartitionKey(companyId), operations);
    }

    public virtual async Task DeleteTransactionAsync(string id, string companyId)
    {
        var container = await GetContainerAsync(TransactionsContainerName);
        await container.DeleteItemAsync<BankTransaction>(id, GetTransactionPartitionKey(companyId));
    }

    public virtual async Task<int> GetUncategorizedCountAsync(string companyId)
    {
        var container = await GetContainerAsync(TransactionsContainerName);
        var query = new QueryDefinition(
            "SELECT VALUE COUNT(1) FROM c WHERE c.CompanyId = @companyId AND c.Type = 'BankTransaction' AND c.Status = @status")
            .WithParameter("@companyId", companyId)
            .WithParameter("@status", (int)BankTransactionStatus.Uncategorized);

        using var iterator = container.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        return response.FirstOrDefault();
    }

    public virtual async Task BulkUpsertTransactionsAsync(List<BankTransaction> transactions)
    {
        if (transactions.Count == 0) return;

        var container = await GetContainerAsync(TransactionsContainerName);
        var tasks = new List<Task>();

        foreach (var txn in transactions)
        {
            if (string.IsNullOrEmpty(txn.id))
                txn.id = GenerateId(TransactionIdPrefix);

            tasks.Add(container.UpsertItemAsync(txn, GetTransactionPartitionKey(txn.CompanyId)));

            // Batch in groups of 20 to avoid throttling
            if (tasks.Count >= 20)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }
}
