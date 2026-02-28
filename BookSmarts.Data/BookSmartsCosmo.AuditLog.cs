using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string AuditLogContainerName = "AuditLog";
    public const string AuditLogPartitionKeyName = "CompanyId";
    public const string AuditLogIdPrefix = "aud";

    public PartitionKey GetAuditLogPartitionKey(string companyId) => new(companyId);

    public virtual async Task<AuditEntry> CreateAuditEntryAsync(AuditEntry entry)
    {
        if (string.IsNullOrEmpty(entry.id))
            entry.id = GenerateId(AuditLogIdPrefix);
        entry.TimestampUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(AuditLogContainerName);
        var response = await container.CreateItemAsync(entry, GetAuditLogPartitionKey(entry.CompanyId));
        return response.Resource;
    }

    public virtual async Task<List<AuditEntry>> GetAuditEntriesAsync(
        string companyId, DateTime? fromDate = null, DateTime? toDate = null,
        string? entityType = null, string? entityId = null, int maxItems = 100)
    {
        var container = await GetContainerAsync(AuditLogContainerName);
        var conditions = new List<string>
        {
            "c.CompanyId = @companyId",
            "c.Type = 'AuditEntry'"
        };
        var queryDef = new QueryDefinition("");

        if (fromDate.HasValue)
            conditions.Add("c.TimestampUtc >= @fromDate");
        if (toDate.HasValue)
            conditions.Add("c.TimestampUtc <= @toDate");
        if (!string.IsNullOrEmpty(entityType))
            conditions.Add("c.EntityType = @entityType");
        if (!string.IsNullOrEmpty(entityId))
            conditions.Add("c.EntityId = @entityId");

        var sql = $"SELECT TOP @maxItems * FROM c WHERE {string.Join(" AND ", conditions)} ORDER BY c.TimestampUtc DESC";
        queryDef = new QueryDefinition(sql)
            .WithParameter("@companyId", companyId)
            .WithParameter("@maxItems", maxItems);

        if (fromDate.HasValue)
            queryDef = queryDef.WithParameter("@fromDate", fromDate.Value.ToString("o"));
        if (toDate.HasValue)
            queryDef = queryDef.WithParameter("@toDate", toDate.Value.ToString("o"));
        if (!string.IsNullOrEmpty(entityType))
            queryDef = queryDef.WithParameter("@entityType", entityType);
        if (!string.IsNullOrEmpty(entityId))
            queryDef = queryDef.WithParameter("@entityId", entityId);

        var results = new List<AuditEntry>();
        using var iterator = container.GetItemQueryIterator<AuditEntry>(queryDef);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }
}
