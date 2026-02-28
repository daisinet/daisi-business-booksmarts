using BookSmarts.Core.Models;
using Microsoft.Azure.Cosmos;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo
{
    public const string OrganizationsIdPrefix = "org";
    public const string DivisionIdPrefix = "div";
    public const string CompanyIdPrefix = "co";
    public const string OrganizationsContainerName = "Organizations";
    public const string OrganizationsPartitionKeyName = nameof(Organization.AccountId);

    public PartitionKey GetPartitionKey(Organization org) => new(org.AccountId);
    public PartitionKey GetPartitionKey(Company company) => new(company.AccountId);

    // Organizations

    public virtual async Task<Organization> CreateOrganizationAsync(Organization org)
    {
        if (string.IsNullOrEmpty(org.id))
            org.id = GenerateId(OrganizationsIdPrefix);
        org.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.CreateItemAsync(org, GetPartitionKey(org));
        return response.Resource;
    }

    public virtual async Task<Organization?> GetOrganizationAsync(string id, string accountId)
    {
        try
        {
            var container = await GetContainerAsync(OrganizationsContainerName);
            var response = await container.ReadItemAsync<Organization>(id, new PartitionKey(accountId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Organization>> GetOrganizationsAsync(string accountId)
    {
        var container = await GetContainerAsync(OrganizationsContainerName);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.AccountId = @accountId AND c.Type = 'Organization'")
            .WithParameter("@accountId", accountId);

        var results = new List<Organization>();
        using var iterator = container.GetItemQueryIterator<Organization>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<Organization> UpdateOrganizationAsync(Organization org)
    {
        org.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.UpsertItemAsync(org, GetPartitionKey(org));
        return response.Resource;
    }

    // Companies

    public virtual async Task<Company> CreateCompanyAsync(Company company)
    {
        if (string.IsNullOrEmpty(company.id))
            company.id = GenerateId(CompanyIdPrefix);
        company.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.CreateItemAsync(company, GetPartitionKey(company));
        return response.Resource;
    }

    public virtual async Task<Company?> GetCompanyAsync(string id, string accountId)
    {
        try
        {
            var container = await GetContainerAsync(OrganizationsContainerName);
            var response = await container.ReadItemAsync<Company>(id, new PartitionKey(accountId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public virtual async Task<List<Company>> GetCompaniesAsync(string accountId, string? organizationId = null)
    {
        var container = await GetContainerAsync(OrganizationsContainerName);

        var sql = "SELECT * FROM c WHERE c.AccountId = @accountId AND c.Type = 'Company'";
        if (!string.IsNullOrEmpty(organizationId))
            sql += " AND c.OrganizationId = @orgId";

        var query = new QueryDefinition(sql)
            .WithParameter("@accountId", accountId);

        if (!string.IsNullOrEmpty(organizationId))
            query = query.WithParameter("@orgId", organizationId);

        var results = new List<Company>();
        using var iterator = container.GetItemQueryIterator<Company>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<Company> UpdateCompanyAsync(Company company)
    {
        company.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.UpsertItemAsync(company, GetPartitionKey(company));
        return response.Resource;
    }

    // Divisions

    public virtual async Task<Division> CreateDivisionAsync(Division division)
    {
        if (string.IsNullOrEmpty(division.id))
            division.id = GenerateId(DivisionIdPrefix);
        division.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.CreateItemAsync(division, new PartitionKey(division.AccountId));
        return response.Resource;
    }

    public virtual async Task<List<Division>> GetDivisionsAsync(string accountId, string organizationId)
    {
        var container = await GetContainerAsync(OrganizationsContainerName);
        var query = new QueryDefinition("SELECT * FROM c WHERE c.AccountId = @accountId AND c.OrganizationId = @orgId AND c.Type = 'Division'")
            .WithParameter("@accountId", accountId)
            .WithParameter("@orgId", organizationId);

        var results = new List<Division>();
        using var iterator = container.GetItemQueryIterator<Division>(query);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public virtual async Task<Division> UpdateDivisionAsync(Division division)
    {
        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.UpsertItemAsync(division, new PartitionKey(division.AccountId));
        return response.Resource;
    }

    public virtual async Task DeleteDivisionAsync(string id, string accountId)
    {
        var container = await GetContainerAsync(OrganizationsContainerName);
        await container.DeleteItemAsync<Division>(id, new PartitionKey(accountId));
    }

    // ── EncryptionConfig ──

    public virtual async Task<EncryptionConfig> CreateEncryptionConfigAsync(EncryptionConfig config)
    {
        if (string.IsNullOrEmpty(config.id))
            config.id = GenerateId("enc");
        config.CreatedUtc = DateTime.UtcNow;

        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.CreateItemAsync(config, new PartitionKey(config.AccountId));
        return response.Resource;
    }

    public virtual async Task<EncryptionConfig?> GetEncryptionConfigAsync(string accountId)
    {
        var container = await GetContainerAsync(OrganizationsContainerName);
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.AccountId = @accountId AND c.Type = 'EncryptionConfig'")
            .WithParameter("@accountId", accountId);

        using var iterator = container.GetItemQueryIterator<EncryptionConfig>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public virtual async Task<EncryptionConfig> UpdateEncryptionConfigAsync(EncryptionConfig config)
    {
        config.UpdatedUtc = DateTime.UtcNow;
        var container = await GetContainerAsync(OrganizationsContainerName);
        var response = await container.UpsertItemAsync(config, new PartitionKey(config.AccountId));
        return response.Resource;
    }

    public virtual async Task DeleteEncryptionConfigAsync(string id, string accountId)
    {
        var container = await GetContainerAsync(OrganizationsContainerName);
        await container.DeleteItemAsync<EncryptionConfig>(id, new PartitionKey(accountId));
    }
}
