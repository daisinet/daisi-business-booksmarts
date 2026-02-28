using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string JournalIdPrefix = "je";
    public const string JournalsContainerName = "Journals";
    public const string JournalsPartitionKeyName = nameof(JournalEntry.CompanyId);

    public PartitionKey GetJournalPartitionKey(string companyId) => new(companyId);

    public virtual async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry entry)
    {
        if (string.IsNullOrEmpty(entry.id))
            entry.id = GenerateId(JournalIdPrefix);
        entry.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(JournalsContainerName);
        var response = await container.CreateItemAsync(entry, GetJournalPartitionKey(entry.CompanyId));
        return response.Resource;
    }

    public virtual async Task<JournalEntry?> GetJournalEntryAsync(string id, string companyId)
    {
        try
        {
            var container = await GetContainerAsync(JournalsContainerName);
            var response = await container.ReadItemAsync<JournalEntry>(id, GetJournalPartitionKey(companyId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<JournalEntry>> GetJournalEntriesAsync(string companyId, JournalEntryStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, int maxItems = 100)
    {
        var container = await GetContainerAsync(JournalsContainerName);

        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'JournalEntry'";
        if (status.HasValue)
            sql += " AND c.Status = @status";
        if (fromDate.HasValue)
            sql += " AND c.EntryDate >= @fromDate";
        if (toDate.HasValue)
            sql += " AND c.EntryDate <= @toDate";
        sql += " ORDER BY c.EntryDate DESC";

        var query = new QueryDefinition(sql)
            .WithParameter("@companyId", companyId);

        if (status.HasValue)
            query = query.WithParameter("@status", (int)status.Value);
        if (fromDate.HasValue)
            query = query.WithParameter("@fromDate", fromDate.Value);
        if (toDate.HasValue)
            query = query.WithParameter("@toDate", toDate.Value);

        var results = new List<JournalEntry>();
        using var iterator = container.GetItemQueryIterator<JournalEntry>(query, requestOptions: new QueryRequestOptions { MaxItemCount = maxItems });
        while (iterator.HasMoreResults && results.Count < maxItems)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results.Take(maxItems).ToList();
    }

    public virtual async Task<JournalEntry> UpdateJournalEntryAsync(JournalEntry entry)
    {
        var container = await GetContainerAsync(JournalsContainerName);
        var response = await container.UpsertItemAsync(entry, GetJournalPartitionKey(entry.CompanyId));
        return response.Resource;
    }

    public virtual async Task PatchJournalEntryStatusAsync(string id, string companyId, JournalEntryStatus status, string? postedBy = null)
    {
        var container = await GetContainerAsync(JournalsContainerName);
        var operations = new List<PatchOperation>
        {
            PatchOperation.Set("/Status", (int)status)
        };

        if (status == JournalEntryStatus.Posted)
        {
            operations.Add(PatchOperation.Set("/PostedUtc", DateTime.UtcNow));
            if (postedBy != null)
                operations.Add(PatchOperation.Set("/PostedBy", postedBy));
        }
        else if (status == JournalEntryStatus.Voided)
        {
            operations.Add(PatchOperation.Set("/VoidedUtc", DateTime.UtcNow));
        }

        await container.PatchItemAsync<JournalEntry>(id, GetJournalPartitionKey(companyId), operations);
    }

    public virtual async Task<int> GetNextEntryNumberAsync(string companyId)
    {
        var container = await GetContainerAsync(JournalsContainerName);
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.CompanyId = @companyId AND c.Type = 'JournalEntry'")
            .WithParameter("@companyId", companyId);

        using var iterator = container.GetItemQueryIterator<int>(query);
        var response = await iterator.ReadNextAsync();
        return response.FirstOrDefault() + 1;
    }

    /// <summary>
    /// Gets posted journal entries within a date range. Used by Income Statement and Cash Flow.
    /// </summary>
    public virtual async Task<List<JournalEntry>> GetPostedJournalEntriesForRangeAsync(
        string companyId, DateTime fromDate, DateTime toDate, bool? cashBasis = null)
    {
        var container = await GetContainerAsync(JournalsContainerName);

        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'JournalEntry' AND c.Status = @posted AND c.EntryDate >= @fromDate AND c.EntryDate <= @toDate";
        if (cashBasis == true)
            sql += " AND c.SourceType IN (0, 1, 4, 6, 7)"; // Manual, BankImport, Payment, Adjustment, Reversal

        var query = new QueryDefinition(sql)
            .WithParameter("@companyId", companyId)
            .WithParameter("@posted", (int)JournalEntryStatus.Posted)
            .WithParameter("@fromDate", fromDate)
            .WithParameter("@toDate", toDate);

        var results = new List<JournalEntry>();
        using var iterator = container.GetItemQueryIterator<JournalEntry>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    /// <summary>
    /// Gets all posted journal entries for trial balance calculation.
    /// </summary>
    public virtual async Task<List<JournalEntry>> GetPostedJournalEntriesAsync(string companyId, DateTime? asOfDate = null, bool? cashBasis = null)
    {
        var container = await GetContainerAsync(JournalsContainerName);

        var sql = "SELECT * FROM c WHERE c.CompanyId = @companyId AND c.Type = 'JournalEntry' AND c.Status = @posted";
        if (asOfDate.HasValue)
            sql += " AND c.EntryDate <= @asOfDate";
        if (cashBasis == true)
            sql += " AND c.SourceType IN (0, 1, 4, 6, 7)"; // Manual, BankImport, Payment, Adjustment, Reversal

        var query = new QueryDefinition(sql)
            .WithParameter("@companyId", companyId)
            .WithParameter("@posted", (int)JournalEntryStatus.Posted);

        if (asOfDate.HasValue)
            query = query.WithParameter("@asOfDate", asOfDate.Value);

        var results = new List<JournalEntry>();
        using var iterator = container.GetItemQueryIterator<JournalEntry>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }
}
