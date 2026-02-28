using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string InterCompanyIdPrefix = "ic";

    public PartitionKey GetInterCompanyPartitionKey(string organizationId) => new(organizationId);

    public virtual async Task<InterCompanyTransaction> CreateInterCompanyTransactionAsync(InterCompanyTransaction transaction)
    {
        if (string.IsNullOrEmpty(transaction.id))
            transaction.id = GenerateId(InterCompanyIdPrefix);
        transaction.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(InterCompanyContainerName);
        var response = await container.CreateItemAsync(transaction, GetInterCompanyPartitionKey(transaction.OrganizationId));
        return response.Resource;
    }

    public virtual async Task<InterCompanyTransaction?> GetInterCompanyTransactionAsync(string id, string organizationId)
    {
        try
        {
            var container = await GetContainerAsync(InterCompanyContainerName);
            var response = await container.ReadItemAsync<InterCompanyTransaction>(id, GetInterCompanyPartitionKey(organizationId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<InterCompanyTransaction>> GetInterCompanyTransactionsAsync(
        string organizationId, InterCompanyStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var queryText = "SELECT * FROM c WHERE c.OrganizationId = @orgId AND c.Type = 'InterCompanyTransaction'";

        if (status.HasValue)
            queryText += " AND c.Status = @status";
        if (fromDate.HasValue)
            queryText += " AND c.TransactionDate >= @fromDate";
        if (toDate.HasValue)
            queryText += " AND c.TransactionDate <= @toDate";

        queryText += " ORDER BY c.TransactionDate DESC";

        var query = new QueryDefinition(queryText)
            .WithParameter("@orgId", organizationId);

        if (status.HasValue)
            query = query.WithParameter("@status", (int)status.Value);
        if (fromDate.HasValue)
            query = query.WithParameter("@fromDate", fromDate.Value);
        if (toDate.HasValue)
            query = query.WithParameter("@toDate", toDate.Value);

        var container = await GetContainerAsync(InterCompanyContainerName);
        var results = new List<InterCompanyTransaction>();
        using var iterator = container.GetItemQueryIterator<InterCompanyTransaction>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<List<InterCompanyTransaction>> GetInterCompanyTransactionsForCompanyAsync(string organizationId, string companyId)
    {
        var container = await GetContainerAsync(InterCompanyContainerName);
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.OrganizationId = @orgId AND c.Type = 'InterCompanyTransaction' AND (c.SourceCompanyId = @companyId OR c.TargetCompanyId = @companyId) ORDER BY c.TransactionDate DESC")
            .WithParameter("@orgId", organizationId)
            .WithParameter("@companyId", companyId);

        var results = new List<InterCompanyTransaction>();
        using var iterator = container.GetItemQueryIterator<InterCompanyTransaction>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<List<InterCompanyTransaction>> GetPostedInterCompanyTransactionsAsync(string organizationId, DateTime? asOfDate = null)
    {
        var queryText = "SELECT * FROM c WHERE c.OrganizationId = @orgId AND c.Type = 'InterCompanyTransaction' AND c.Status = 0 AND c.EliminateOnConsolidation = true";

        if (asOfDate.HasValue)
            queryText += " AND c.TransactionDate <= @asOfDate";

        var query = new QueryDefinition(queryText)
            .WithParameter("@orgId", organizationId);

        if (asOfDate.HasValue)
            query = query.WithParameter("@asOfDate", asOfDate.Value);

        var container = await GetContainerAsync(InterCompanyContainerName);
        var results = new List<InterCompanyTransaction>();
        using var iterator = container.GetItemQueryIterator<InterCompanyTransaction>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<InterCompanyTransaction> UpdateInterCompanyTransactionAsync(InterCompanyTransaction transaction)
    {
        var container = await GetContainerAsync(InterCompanyContainerName);
        var response = await container.UpsertItemAsync(transaction, GetInterCompanyPartitionKey(transaction.OrganizationId));
        return response.Resource;
    }
}
