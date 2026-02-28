using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string BookSmartsUserIdPrefix = "bsu";

    // BookSmartsUser is stored in the Organizations container (partitioned by AccountId)

    public virtual async Task<BookSmartsUser> CreateBookSmartsUserAsync(BookSmartsUser user)
    {
        if (string.IsNullOrEmpty(user.id))
            user.id = GenerateId(BookSmartsUserIdPrefix);
        user.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.CreateItemAsync(user, new PartitionKey(user.AccountId));
        return response.Resource;
    }

    public virtual async Task<BookSmartsUser?> GetBookSmartsUserAsync(string id, string accountId)
    {
        try
        {
            var container = await GetContainerAsync(OrganizationsContainerName);
            var response = await container.ReadItemAsync<BookSmartsUser>(id, new PartitionKey(accountId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<BookSmartsUser?> GetBookSmartsUserByDaisinetIdAsync(string daisinetUserId, string accountId)
    {
        var container = await GetContainerAsync(OrganizationsContainerName);
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.AccountId = @accountId AND c.Type = 'BookSmartsUser' AND c.DaisinetUserId = @daisinetUserId")
            .WithParameter("@accountId", accountId)
            .WithParameter("@daisinetUserId", daisinetUserId);

        using var iterator = container.GetItemQueryIterator<BookSmartsUser>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public virtual async Task<List<BookSmartsUser>> GetBookSmartsUsersAsync(string accountId, bool activeOnly = false)
    {
        var container = await GetContainerAsync(OrganizationsContainerName);

        var sql = "SELECT * FROM c WHERE c.AccountId = @accountId AND c.Type = 'BookSmartsUser'";
        if (activeOnly)
            sql += " AND c.IsActive = true";

        var query = new QueryDefinition(sql)
            .WithParameter("@accountId", accountId);

        var results = new List<BookSmartsUser>();
        using var iterator = container.GetItemQueryIterator<BookSmartsUser>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<BookSmartsUser> UpdateBookSmartsUserAsync(BookSmartsUser user)
    {
        user.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.UpsertItemAsync(user, new PartitionKey(user.AccountId));
        return response.Resource;
    }
}
