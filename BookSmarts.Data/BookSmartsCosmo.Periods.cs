using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string FiscalYearIdPrefix = "fy";
    public const string PeriodsContainerName = "Periods";
    public const string PeriodsPartitionKeyName = nameof(FiscalYear.CompanyId);

    // Placeholder container names for future phases
    public const string BudgetsContainerName = "Budgets";
    public const string BudgetsPartitionKeyName = "CompanyId";
    public const string ReconciliationContainerName = "Reconciliation";
    public const string ReconciliationPartitionKeyName = "CompanyId";
    public const string InterCompanyContainerName = "InterCompany";
    public const string InterCompanyPartitionKeyName = "OrganizationId";

    public PartitionKey GetPeriodPartitionKey(string companyId) => new(companyId);

    public virtual async Task<FiscalYear> CreateFiscalYearAsync(FiscalYear fiscalYear)
    {
        if (string.IsNullOrEmpty(fiscalYear.id))
            fiscalYear.id = GenerateId(FiscalYearIdPrefix);
        fiscalYear.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(PeriodsContainerName);
        var response = await container.CreateItemAsync(fiscalYear, GetPeriodPartitionKey(fiscalYear.CompanyId));
        return response.Resource;
    }

    public virtual async Task<FiscalYear?> GetFiscalYearAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(PeriodsContainerName);
            var response = await container.ReadItemAsync<FiscalYear>(id, GetPeriodPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<FiscalYear>> GetFiscalYearsAsync(string companyId)
    {
        var container = await GetContainerAsync(PeriodsContainerName);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'FiscalYear' ORDER BY c.StartDate DESC")
            .WithParameter("@companyId", companyId);

        var results = new List<FiscalYear>();
        using var iterator = container.GetItemQueryIterator<FiscalYear>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<FiscalYear> UpdateFiscalYearAsync(FiscalYear fiscalYear)
    {
        var container = await GetContainerAsync(PeriodsContainerName);
        var response = await container.UpsertItemAsync(fiscalYear, GetPeriodPartitionKey(fiscalYear.CompanyId));
        return response.Resource;
    }

    public virtual async Task<FiscalYear?> GetFiscalYearForDateAsync(string companyId, DateTime date)
    {
        var container = await GetContainerAsync(PeriodsContainerName);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'FiscalYear' AND c.StartDate <= @date AND c.EndDate >= @date")
            .WithParameter("@companyId", companyId)
            .WithParameter("@date", date);

        using var iterator = container.GetItemQueryIterator<FiscalYear>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }
}
