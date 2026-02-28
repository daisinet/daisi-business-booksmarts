using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string BankConnectionIdPrefix = "bc";
    public const string CategorizationRuleIdPrefix = "cr";
    public const string BankingContainerName = "Banking";
    public const string BankingPartitionKeyName = "CompanyId";

    public PartitionKey GetBankingPartitionKey(string companyId) => new(companyId);

    // ── BankConnection CRUD ──

    public virtual async Task<BankConnection> CreateBankConnectionAsync(BankConnection connection)
    {
        if (string.IsNullOrEmpty(connection.id))
            connection.id = GenerateId(BankConnectionIdPrefix);
        connection.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(BankingContainerName);
        var response = await container.CreateItemAsync(connection, GetBankingPartitionKey(connection.CompanyId));
        return response.Resource;
    }

    public virtual async Task<BankConnection?> GetBankConnectionAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(BankingContainerName);
            var response = await container.ReadItemAsync<BankConnection>(id, GetBankingPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<BankConnection>> GetBankConnectionsAsync(string companyId)
    {
        var container = await GetContainerAsync(BankingContainerName);
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'BankConnection' ORDER BY c.CreatedUtc DESC")
            .WithParameter("@companyId", companyId);

        var results = new List<BankConnection>();
        using var iterator = container.GetItemQueryIterator<BankConnection>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<List<BankConnection>> GetAllActiveBankConnectionsAsync()
    {
        var container = await GetContainerAsync(BankingContainerName);
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.Type = 'BankConnection' AND c.Status = @status")
            .WithParameter("@status", (int)BankConnectionStatus.Active);

        var results = new List<BankConnection>();
        using var iterator = container.GetItemQueryIterator<BankConnection>(query,
            requestOptions: new QueryRequestOptions { MaxItemCount = 100 });
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<BankConnection?> GetBankConnectionByPlaidItemIdAsync(string plaidItemId)
    {
        var container = await GetContainerAsync(BankingContainerName);
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.Type = 'BankConnection' AND c.PlaidItemId = @plaidItemId")
            .WithParameter("@plaidItemId", plaidItemId);

        using var iterator = container.GetItemQueryIterator<BankConnection>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public virtual async Task<BankConnection> UpdateBankConnectionAsync(BankConnection connection)
    {
        connection.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(BankingContainerName);
        var response = await container.UpsertItemAsync(connection, GetBankingPartitionKey(connection.CompanyId));
        return response.Resource;
    }

    public virtual async Task DeleteBankConnectionAsync(string id, string companyId)
    {
        var container = await GetContainerAsync(BankingContainerName);
        await container.DeleteItemAsync<BankConnection>(id, GetBankingPartitionKey(companyId));
    }

    public virtual async Task PatchBankConnectionCursorAsync(string id, string companyId, string? cursor)
    {
        var container = await GetContainerAsync(BankingContainerName);
        var operations = new List<PatchOperation>
        {
            PatchOperation.Set("/TransactionsCursor", cursor),
            PatchOperation.Set("/LastSyncUtc", DateTime.UtcNow),
            PatchOperation.Set("/UpdatedUtc", DateTime.UtcNow)
        };
        await container.PatchItemAsync<BankConnection>(id, GetBankingPartitionKey(companyId), operations);
    }

    public virtual async Task PatchBankConnectionWebhookAsync(string id, string companyId)
    {
        var container = await GetContainerAsync(BankingContainerName);
        var operations = new List<PatchOperation>
        {
            PatchOperation.Set("/LastWebhookUtc", DateTime.UtcNow)
        };
        await container.PatchItemAsync<BankConnection>(id, GetBankingPartitionKey(companyId), operations);
    }

    public virtual async Task PatchBankConnectionStatusAsync(string id, string companyId, BankConnectionStatus status, string? errorCode = null, string? errorMessage = null)
    {
        var container = await GetContainerAsync(BankingContainerName);
        var operations = new List<PatchOperation>
        {
            PatchOperation.Set("/Status", (int)status),
            PatchOperation.Set("/UpdatedUtc", DateTime.UtcNow)
        };

        if (status == BankConnectionStatus.Error)
        {
            operations.Add(PatchOperation.Set("/ErrorCode", errorCode));
            operations.Add(PatchOperation.Set("/ErrorMessage", errorMessage));
        }
        else if (status == BankConnectionStatus.Active)
        {
            operations.Add(PatchOperation.Set("/ErrorCode", (string?)null));
            operations.Add(PatchOperation.Set("/ErrorMessage", (string?)null));
        }

        await container.PatchItemAsync<BankConnection>(id, GetBankingPartitionKey(companyId), operations);
    }

    // ── CategorizationRule CRUD ──

    public virtual async Task<CategorizationRule> CreateRuleAsync(CategorizationRule rule)
    {
        if (string.IsNullOrEmpty(rule.id))
            rule.id = GenerateId(CategorizationRuleIdPrefix);
        rule.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(BankingContainerName);
        var response = await container.CreateItemAsync(rule, GetBankingPartitionKey(rule.CompanyId));
        return response.Resource;
    }

    public virtual async Task<CategorizationRule?> GetRuleAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(BankingContainerName);
            var response = await container.ReadItemAsync<CategorizationRule>(id, GetBankingPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<CategorizationRule>> GetRulesAsync(string companyId, bool activeOnly = false)
    {
        var container = await GetContainerAsync(BankingContainerName);
        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'CategorizationRule'";
        if (activeOnly)
            sql += " AND c.IsActive = true";
        sql += " ORDER BY c.Priority ASC";

        var query = new QueryDefinition(sql)
            .WithParameter("@companyId", companyId);

        var results = new List<CategorizationRule>();
        using var iterator = container.GetItemQueryIterator<CategorizationRule>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<CategorizationRule> UpdateRuleAsync(CategorizationRule rule)
    {
        rule.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(BankingContainerName);
        var response = await container.UpsertItemAsync(rule, GetBankingPartitionKey(rule.CompanyId));
        return response.Resource;
    }

    public virtual async Task DeleteRuleAsync(string id, string companyId)
    {
        var container = await GetContainerAsync(BankingContainerName);
        await container.DeleteItemAsync<CategorizationRule>(id, GetBankingPartitionKey(companyId));
    }

    public virtual async Task IncrementRuleAppliedCountAsync(string id, string companyId)
    {
        var container = await GetContainerAsync(BankingContainerName);
        var operations = new List<PatchOperation>
        {
            PatchOperation.Increment("/TimesApplied", 1)
        };
        await container.PatchItemAsync<CategorizationRule>(id, GetBankingPartitionKey(companyId), operations);
    }
}
