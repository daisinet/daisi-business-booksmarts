using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string CustomReportsContainerName = "CustomReports";
    public const string CustomReportsPartitionKeyName = "CompanyId";
    public const string CustomReportIdPrefix = "rpt";

    public PartitionKey GetCustomReportPartitionKey(string companyId) => new(companyId);

    public virtual async Task<CustomReport> CreateCustomReportAsync(CustomReport report)
    {
        if (string.IsNullOrEmpty(report.id))
            report.id = GenerateId(CustomReportIdPrefix);
        report.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(CustomReportsContainerName);
        var response = await container.CreateItemAsync(report, GetCustomReportPartitionKey(report.CompanyId));
        return response.Resource;
    }

    public virtual async Task<CustomReport?> GetCustomReportAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(CustomReportsContainerName);
            var response = await container.ReadItemAsync<CustomReport>(id, GetCustomReportPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<CustomReport>> GetCustomReportsAsync(string companyId)
    {
        var container = await GetContainerAsync(CustomReportsContainerName);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'CustomReport' ORDER BY c.Name")
            .WithParameter("@companyId", companyId);

        var results = new List<CustomReport>();
        using var iterator = container.GetItemQueryIterator<CustomReport>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<CustomReport> UpdateCustomReportAsync(CustomReport report)
    {
        report.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(CustomReportsContainerName);
        var response = await container.UpsertItemAsync(report, GetCustomReportPartitionKey(report.CompanyId));
        return response.Resource;
    }

    public virtual async Task DeleteCustomReportAsync(string id, string companyId)
    {
        var container = await GetContainerAsync(CustomReportsContainerName);
        await container.DeleteItemAsync<CustomReport>(id, GetCustomReportPartitionKey(companyId));
    }
}
