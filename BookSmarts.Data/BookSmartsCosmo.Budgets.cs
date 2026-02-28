using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string BudgetIdPrefix = "bud";

    public PartitionKey GetBudgetPartitionKey(string companyId) => new(companyId);

    public virtual async Task<Budget> CreateBudgetAsync(Budget budget)
    {
        if (string.IsNullOrEmpty(budget.id))
            budget.id = GenerateId(BudgetIdPrefix);
        budget.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(BudgetsContainerName);
        var response = await container.CreateItemAsync(budget, GetBudgetPartitionKey(budget.CompanyId));
        return response.Resource;
    }

    public virtual async Task<Budget?> GetBudgetAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(BudgetsContainerName);
            var response = await container.ReadItemAsync<Budget>(id, GetBudgetPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Budget>> GetBudgetsAsync(string companyId)
    {
        var container = await GetContainerAsync(BudgetsContainerName);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Budget' ORDER BY c.CreatedUtc DESC")
            .WithParameter("@companyId", companyId);

        var results = new List<Budget>();
        using var iterator = container.GetItemQueryIterator<Budget>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<Budget?> GetBudgetForFiscalYearAsync(string companyId, string fiscalYearId)
    {
        var container = await GetContainerAsync(BudgetsContainerName);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'Budget' AND c.FiscalYearId = @fyId")
            .WithParameter("@companyId", companyId)
            .WithParameter("@fyId", fiscalYearId);

        using var iterator = container.GetItemQueryIterator<Budget>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public virtual async Task<Budget> UpdateBudgetAsync(Budget budget)
    {
        budget.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(BudgetsContainerName);
        var response = await container.UpsertItemAsync(budget, GetBudgetPartitionKey(budget.CompanyId));
        return response.Resource;
    }

    public virtual async Task DeleteBudgetAsync(string id, string companyId)
    {
        var container = await GetContainerAsync(BudgetsContainerName);
        await container.DeleteItemAsync<Budget>(id, GetBudgetPartitionKey(companyId));
    }
}
