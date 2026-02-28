using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace BookSmarts.Data;

public partial class BookSmartsCosmo(IConfiguration configuration, string connectionStringConfigurationName = "Cosmo:ConnectionString")
{
    private readonly Lazy<CosmosClient> _client = new(() =>
    {
        var connectionString = configuration[connectionStringConfigurationName];
        var options = new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            }
        };
        return new CosmosClient(connectionString, options);
    });

    private readonly ConcurrentDictionary<string, Container> _containerCache = new();

    public static string GenerateId(string prefix)
    {
        return $"{prefix}-{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    private const string DatabaseName = "daisi-booksmarts";

    private Database? _database;

    public CosmosClient GetCosmoClient() => _client.Value;

    public async Task<Database> GetDatabaseAsync()
    {
        if (_database != null)
            return _database;

        var response = await GetCosmoClient().CreateDatabaseIfNotExistsAsync(DatabaseName);
        _database = response.Database;
        return _database;
    }

    public async Task<Container> GetContainerAsync(string containerName)
    {
        if (_containerCache.TryGetValue(containerName, out var cached))
            return cached;

        string partitionKeyPath = "/" + containerName switch
        {
            OrganizationsContainerName => OrganizationsPartitionKeyName,
            ChartOfAccountsContainerName => ChartOfAccountsPartitionKeyName,
            JournalsContainerName => JournalsPartitionKeyName,
            PeriodsContainerName => PeriodsPartitionKeyName,
            TransactionsContainerName => TransactionsPartitionKeyName,
            BankingContainerName => BankingPartitionKeyName,
            ArApContainerName => ArApPartitionKeyName,
            BudgetsContainerName => BudgetsPartitionKeyName,
            ReconciliationContainerName => ReconciliationPartitionKeyName,
            InterCompanyContainerName => InterCompanyPartitionKeyName,
            AuditLogContainerName => AuditLogPartitionKeyName,
            CustomReportsContainerName => CustomReportsPartitionKeyName,
            _ => "id"
        };

        var db = await GetDatabaseAsync();
        var container = await db.CreateContainerIfNotExistsAsync(containerName, partitionKeyPath);
        _containerCache.TryAdd(containerName, container);
        return container;
    }
}
