using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string CoaIdPrefix = "coa";
    public const string ChartOfAccountsContainerName = "ChartOfAccounts";
    public const string ChartOfAccountsPartitionKeyName = nameof(ChartOfAccountEntry.CompanyId);

    public PartitionKey GetCoaPartitionKey(string companyId) => new(companyId);

    public virtual async Task<ChartOfAccountEntry> CreateAccountAsync(ChartOfAccountEntry entry)
    {
        if (string.IsNullOrEmpty(entry.id))
            entry.id = GenerateId(CoaIdPrefix);
        entry.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(ChartOfAccountsContainerName);
        var response = await container.CreateItemAsync(entry, GetCoaPartitionKey(entry.CompanyId));
        return response.Resource;
    }

    public virtual async Task<ChartOfAccountEntry?> GetAccountEntryAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(ChartOfAccountsContainerName);
            var response = await container.ReadItemAsync<ChartOfAccountEntry>(id, GetCoaPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<ChartOfAccountEntry>> GetChartOfAccountsAsync(string companyId, bool activeOnly = true)
    {
        var container = await GetContainerAsync(ChartOfAccountsContainerName);

        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId";
        if (activeOnly)
            sql += " AND c.IsActive = true";
        sql += " ORDER BY c.AccountNumber";

        var query = new QueryDefinition(sql)
            .WithParameter("@companyId", companyId);

        var results = new List<ChartOfAccountEntry>();
        using var iterator = container.GetItemQueryIterator<ChartOfAccountEntry>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<ChartOfAccountEntry> UpdateAccountEntryAsync(ChartOfAccountEntry entry)
    {
        entry.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(ChartOfAccountsContainerName);
        var response = await container.UpsertItemAsync(entry, GetCoaPartitionKey(entry.CompanyId));
        return response.Resource;
    }

    public virtual async Task<bool> AccountNumberExistsAsync(string companyId, string accountNumber, string? excludeId = null)
    {
        var container = await GetContainerAsync(ChartOfAccountsContainerName);

        var sql = "SELECT VALUE COUNT(1) FROM c WHERE c.CompanyId = @companyId AND c.AccountNumber = @number";
        if (!string.IsNullOrEmpty(excludeId))
            sql += " AND c.id != @excludeId";

        var query = new QueryDefinition(sql)
            .WithParameter("@companyId", companyId)
            .WithParameter("@number", accountNumber);

        if (!string.IsNullOrEmpty(excludeId))
            query = query.WithParameter("@excludeId", excludeId);

        using var iterator = container.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        return response.FirstOrDefault() > 0;
    }
}
