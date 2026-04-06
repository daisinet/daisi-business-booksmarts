using System.Text.Json;
using Microsoft.Azure.Cosmos;

// ─────────────────────────────────────────────────────────────────
// BookSmarts Cleanup Tool
//
// Automatically identifies E2E test data across two Cosmos databases:
//   - "daisi"            → Accounts starting with "E2E Registration Test"
//                           Users with @test.booksmarts.local emails
//   - "daisi-booksmarts" → All orgs, companies, and child data linked
//                           to those AccountIds
//
// Shows summarized record counts per container with an option to
// expand details, then deletes after explicit confirmation.
// ─────────────────────────────────────────────────────────────────

const string DaisiDb = "daisi";
const string BookSmartsDb = "daisi-booksmarts";
const string E2eAccountPrefix = "E2E Registration Test";
const string E2eEmailDomain = "@test.booksmarts.local";

// Only data created on or after this date is eligible for deletion
var e2eCutoffDate = new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc);
var e2eCutoffStr = e2eCutoffDate.ToString("o"); // ISO 8601 for Cosmos queries

// Containers partitioned by CompanyId
var companyContainers = new[]
{
    "ChartOfAccounts", "Journals", "ArAp", "Banking", "Transactions",
    "Periods", "Budgets", "AuditLog", "CustomReports", "Reconciliation"
};

// ── Get connection string ──
var connectionString = Environment.GetEnvironmentVariable("BOOKSMARTS_COSMOS_CONNECTION");
if (string.IsNullOrEmpty(connectionString))
{
    Console.Write("Cosmos DB connection string: ");
    connectionString = Console.ReadLine()?.Trim();
}
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: No connection string. Set BOOKSMARTS_COSMOS_CONNECTION or enter at prompt.");
    return 1;
}

// ── Connect ──
var clientOptions = new CosmosClientOptions
{
    UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions { PropertyNamingPolicy = null }
};
using var client = new CosmosClient(connectionString, clientOptions);
var daisiDatabase = client.GetDatabase(DaisiDb);
var bsDatabase = client.GetDatabase(BookSmartsDb);

Console.WriteLine();
Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine("  BookSmarts E2E Test Data Cleanup");
Console.WriteLine("════════════════════════════════════════════════════════════════");

// ═══════════════════════════════════════════════════
// Phase 1: DISCOVER — find E2E test accounts in daisi DB
// ═══════════════════════════════════════════════════
Console.WriteLine();
Console.WriteLine("── Phase 1: DISCOVER E2E accounts ─────────────────────────────");
Console.WriteLine();

var daisiAccounts = daisiDatabase.GetContainer("Accounts");

// Allow manual account ID override
var manualAccountId = Environment.GetEnvironmentVariable("BOOKSMARTS_ACCOUNT_ID");
Console.WriteLine("Options:");
Console.WriteLine($"  [Enter]  Auto-detect accounts starting with \"{E2eAccountPrefix}\"");
Console.WriteLine("  [ID]     Enter a specific Account ID to audit");
Console.WriteLine();
if (string.IsNullOrEmpty(manualAccountId))
{
    Console.Write("Account ID (or press Enter for auto-detect): ");
    manualAccountId = Console.ReadLine()?.Trim();
}

List<JsonElement> e2eAccounts;

if (!string.IsNullOrEmpty(manualAccountId))
{
    // Manual mode: look up the specific account
    Console.WriteLine();
    Console.WriteLine($"Looking up account: {manualAccountId}");
    e2eAccounts = await QueryItems(daisiAccounts,
        "SELECT c.id, c.AccountId, c.Name, c.type FROM c WHERE c.type = 'Account' AND (c.AccountId = @aid OR c.id = @aid)",
        ("@aid", manualAccountId));

    if (e2eAccounts.Count == 0)
    {
        Console.WriteLine($"No account found with ID '{manualAccountId}' in daisi database.");
        Console.WriteLine("Will still check daisi-booksmarts for data under this AccountId.");
        // Create a synthetic entry so the audit loop still runs
        var synthetic = JsonDocument.Parse($"{{\"id\":\"{manualAccountId}\",\"AccountId\":\"{manualAccountId}\",\"Name\":\"(manual lookup)\",\"type\":\"Account\"}}");
        e2eAccounts = [synthetic.RootElement];
    }
}
else
{
    // Auto-detect mode
    Console.WriteLine();
    Console.WriteLine($"Auto-detecting accounts starting with \"{E2eAccountPrefix}\"...");
    Console.WriteLine($"Also checking for users with \"{E2eEmailDomain}\" emails");
    Console.WriteLine();

    e2eAccounts = await QueryItems(daisiAccounts,
        "SELECT c.id, c.AccountId, c.Name, c.type FROM c WHERE c.type = 'Account' AND STARTSWITH(c.Name, @prefix)",
        ("@prefix", E2eAccountPrefix));

    if (e2eAccounts.Count == 0)
    {
        Console.WriteLine("No E2E test accounts found. Nothing to clean up.");
        return 0;
    }
}

Console.WriteLine($"Found {e2eAccounts.Count} account(s) to audit:");
Console.WriteLine();

// For each account, get users and gather BookSmarts data
var accountSummaries = new List<AccountSummary>();

foreach (var acct in e2eAccounts)
{
    var acctId = GetStr(acct, "AccountId") ?? GetStr(acct, "id") ?? "";
    var acctName = GetStr(acct, "Name") ?? "—";

    // Get E2E users (with test email domain)
    var allUsers = await QueryItems(daisiAccounts,
        "SELECT c.id, c.Name, c.Email, c.Role, c.Status FROM c WHERE c.AccountId = @aid AND c.type = 'User'",
        ("@aid", acctId));
    var e2eUsers = allUsers.Where(u =>
        (GetStr(u, "Email") ?? "").Contains(E2eEmailDomain, StringComparison.OrdinalIgnoreCase)).ToList();

    // Only accounts whose name starts with E2E prefix are eligible for account+user deletion.
    // Pre-existing accounts will only have their booksmarts data cleaned up.
    var isE2eAccount = acctName.StartsWith(E2eAccountPrefix, StringComparison.OrdinalIgnoreCase);

    var summary = new AccountSummary
    {
        AccountId = acctId,
        AccountName = acctName,
        IsE2eAccount = isE2eAccount,
        DaisiUserCount = allUsers.Count,
        E2eUserCount = e2eUsers.Count,
    };

    Console.WriteLine($"  Account: {acctName}");
    Console.WriteLine($"  ID:      {acctId}");
    Console.WriteLine($"  Users:   {allUsers.Count} total, {e2eUsers.Count} with {E2eEmailDomain}");

    // Query daisi-booksmarts Organizations container for this account
    var orgsContainer = bsDatabase.GetContainer("Organizations");

    var orgs = await QueryItems(orgsContainer,
        "SELECT c.id, c.Name, c.CreatedUtc FROM c WHERE c.AccountId = @aid AND c.Type = 'Organization' AND c.CreatedUtc >= @cutoff",
        ("@aid", acctId), ("@cutoff", e2eCutoffStr));
    summary.OrganizationCount = orgs.Count;
    summary.OrganizationIds = orgs.Select(o => GetStr(o, "id")!).ToList();

    var companies = await QueryItems(orgsContainer,
        "SELECT c.id, c.Name, c.OrganizationId, c.CreatedUtc FROM c WHERE c.AccountId = @aid AND c.Type = 'Company' AND c.CreatedUtc >= @cutoff",
        ("@aid", acctId), ("@cutoff", e2eCutoffStr));
    summary.CompanyCount = companies.Count;
    summary.Companies = companies.Select(c => new CompanyInfo
    {
        Id = GetStr(c, "id")!,
        Name = GetStr(c, "Name") ?? "(encrypted)",
        OrganizationId = GetStr(c, "OrganizationId"),
        CreatedUtc = TryParseDate(c, "CreatedUtc"),
    }).ToList();

    var bsUsers = await QueryItems(orgsContainer,
        "SELECT c.id FROM c WHERE c.AccountId = @aid AND c.Type = 'BookSmartsUser' AND c.CreatedUtc >= @cutoff",
        ("@aid", acctId), ("@cutoff", e2eCutoffStr));
    summary.BookSmartsUserCount = bsUsers.Count;
    summary.BookSmartsUserIds = bsUsers.Select(u => GetStr(u, "id")!).ToList();

    var encConfigs = await QueryItems(orgsContainer,
        "SELECT c.id FROM c WHERE c.AccountId = @aid AND c.Type = 'EncryptionConfig' AND c.CreatedUtc >= @cutoff",
        ("@aid", acctId), ("@cutoff", e2eCutoffStr));
    summary.EncryptionConfigCount = encConfigs.Count;
    summary.EncryptionConfigIds = encConfigs.Select(e => GetStr(e, "id")!).ToList();

    var divisions = await QueryItems(orgsContainer,
        "SELECT c.id FROM c WHERE c.AccountId = @aid AND c.Type = 'Division' AND c.CreatedUtc >= @cutoff",
        ("@aid", acctId), ("@cutoff", e2eCutoffStr));

    // Count child data per container across all companies
    Console.Write("  Counting child data");
    foreach (var co in summary.Companies)
    {
        foreach (var containerName in companyContainers)
        {
            try
            {
                var container = bsDatabase.GetContainer(containerName);
                var countResult = await QueryItems(container,
                    "SELECT VALUE COUNT(1) FROM c WHERE c.CompanyId = @cid AND c.CreatedUtc >= @cutoff",
                    ("@cid", co.Id), ("@cutoff", e2eCutoffStr));
                var count = countResult.Count > 0 ? countResult[0].GetInt32() : 0;
                co.ChildCounts[containerName] = count;
            }
            catch (CosmosException)
            {
                co.ChildCounts[containerName] = 0;
            }
        }

        // InterCompany
        if (!string.IsNullOrEmpty(co.OrganizationId))
        {
            try
            {
                var icContainer = bsDatabase.GetContainer("InterCompany");
                var icResult = await QueryItems(icContainer,
                    "SELECT VALUE COUNT(1) FROM c WHERE c.OrganizationId = @oid AND (c.SourceCompanyId = @cid OR c.TargetCompanyId = @cid) AND c.CreatedUtc >= @cutoff",
                    ("@oid", co.OrganizationId), ("@cid", co.Id), ("@cutoff", e2eCutoffStr));
                co.InterCompanyCount = icResult.Count > 0 ? icResult[0].GetInt32() : 0;
            }
            catch (CosmosException)
            {
                co.InterCompanyCount = 0;
            }
        }
        Console.Write(".");
    }
    Console.WriteLine(" done.");

    accountSummaries.Add(summary);
    Console.WriteLine();
}

// ═══════════════════════════════════════════════════
// Phase 2: SUMMARY — show aggregated counts
// ═══════════════════════════════════════════════════
Console.WriteLine("── Phase 2: SUMMARY ───────────────────────────────────────────");
Console.WriteLine($"   Only data created on or after {e2eCutoffDate:yyyy-MM-dd} is included.");
Console.WriteLine();

var grandTotalItems = 0;

foreach (var summary in accountSummaries)
{
    Console.WriteLine($"┌─ Account: \"{summary.AccountName}\" ({summary.AccountId})");
    Console.WriteLine($"│");
    Console.WriteLine($"│  daisi database:");
    if (summary.IsE2eAccount)
    {
        Console.WriteLine($"│    Account document ........... 1  [WILL DELETE]");
        Console.WriteLine($"│    Users ...................... {summary.DaisiUserCount}  [WILL DELETE]");
        grandTotalItems += 1 + summary.DaisiUserCount;
    }
    else
    {
        Console.WriteLine($"│    Account document ........... 1  [PROTECTED]");
        Console.WriteLine($"│    Users ...................... {summary.DaisiUserCount}  [PROTECTED]");
        // Not counted in grandTotalItems — won't be deleted
    }

    Console.WriteLine($"│");
    Console.WriteLine($"│  daisi-booksmarts Organizations container:");
    Console.WriteLine($"│    Organizations .............. {summary.OrganizationCount}");
    Console.WriteLine($"│    Companies .................. {summary.CompanyCount}");
    Console.WriteLine($"│    BookSmarts Users ........... {summary.BookSmartsUserCount}");
    Console.WriteLine($"│    Encryption Configs ......... {summary.EncryptionConfigCount}");
    Console.WriteLine($"│    Divisions .................. {summary.DivisionCount}");
    var orgContainerTotal = summary.OrganizationCount + summary.CompanyCount
        + summary.BookSmartsUserCount + summary.EncryptionConfigCount + summary.DivisionCount;
    grandTotalItems += orgContainerTotal;

    Console.WriteLine($"│");
    Console.WriteLine($"│  daisi-booksmarts child data (across {summary.CompanyCount} companies):");

    // Aggregate counts per container across all companies
    var containerTotals = new Dictionary<string, int>();
    foreach (var containerName in companyContainers)
    {
        var total = summary.Companies.Sum(c => c.ChildCounts.GetValueOrDefault(containerName));
        containerTotals[containerName] = total;
    }
    var icTotal = summary.Companies.Sum(c => c.InterCompanyCount);

    foreach (var kv in containerTotals.Where(kv => kv.Value > 0))
    {
        Console.WriteLine($"│    {kv.Key,-24} {kv.Value,5}");
        grandTotalItems += kv.Value;
    }
    if (icTotal > 0)
    {
        Console.WriteLine($"│    {"InterCompany",-24} {icTotal,5}");
        grandTotalItems += icTotal;
    }

    var childDataTotal = containerTotals.Values.Sum() + icTotal;
    Console.WriteLine($"│    {"───────────────────",-24} {"─────",5}");
    Console.WriteLine($"│    {"Subtotal",-24} {1 + summary.DaisiUserCount + orgContainerTotal + childDataTotal,5}");
    Console.WriteLine($"└─");
    Console.WriteLine();
}

Console.WriteLine($"  GRAND TOTAL: {grandTotalItems} items across {accountSummaries.Count} account(s)");
Console.WriteLine();

// ── Interactive drill-down ──
// Loop: pick account → pick container → see top 15 records → back
while (true)
{
    Console.WriteLine("── EXPLORE ────────────────────────────────────────────────────");
    if (accountSummaries.Count > 1)
    {
        Console.WriteLine("  Select an account to explore:");
        for (int i = 0; i < accountSummaries.Count; i++)
            Console.WriteLine($"    [{i + 1}] {accountSummaries[i].AccountName}");
    }
    Console.WriteLine("  [0] Continue to delete confirmation");
    Console.WriteLine();
    Console.Write(accountSummaries.Count > 1
        ? $"  Choice (0-{accountSummaries.Count}): "
        : "  Enter 1 to explore the account, or 0 to continue: ");

    var acctInput = Console.ReadLine()?.Trim();
    if (!int.TryParse(acctInput, out var acctIdx) || acctIdx == 0)
        break;
    if (acctIdx < 1 || acctIdx > accountSummaries.Count)
        continue;

    var acctSummary = accountSummaries[acctIdx - 1];

    // Account-level drill-down: show containers with data
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine($"── Account: \"{acctSummary.AccountName}\" ──");
        Console.WriteLine();

        // Build a list of containers that have data
        var drillOptions = new List<(string Label, string Container, string Query, string PartitionSource)>();

        // daisi database items
        if (acctSummary.DaisiUserCount > 0)
            drillOptions.Add(($"daisi / Users ({acctSummary.DaisiUserCount})", "daisi:Accounts",
                "SELECT TOP 15 c.id, c.Name, c.Email, c.Role, c.Status FROM c WHERE c.AccountId = @aid AND c.type = 'User'",
                "daisi"));

        // Organizations container items
        if (acctSummary.OrganizationCount > 0)
            drillOptions.Add(($"Organizations ({acctSummary.OrganizationCount})", "bs:Organizations",
                "SELECT TOP 15 c.id, c.Name, c.CreatedUtc FROM c WHERE c.AccountId = @aid AND c.Type = 'Organization'",
                "bs-org"));
        if (acctSummary.CompanyCount > 0)
            drillOptions.Add(($"Companies ({acctSummary.CompanyCount})", "bs:Organizations",
                "SELECT TOP 15 c.id, c.Name, c.OrganizationId, c.CreatedUtc FROM c WHERE c.AccountId = @aid AND c.Type = 'Company'",
                "bs-org"));
        if (acctSummary.BookSmartsUserCount > 0)
            drillOptions.Add(($"BookSmarts Users ({acctSummary.BookSmartsUserCount})", "bs:Organizations",
                "SELECT TOP 15 c.id, c.Name, c.DaisinetUserId, c.Role, c.CreatedUtc FROM c WHERE c.AccountId = @aid AND c.Type = 'BookSmartsUser'",
                "bs-org"));
        if (acctSummary.EncryptionConfigCount > 0)
            drillOptions.Add(($"Encryption Configs ({acctSummary.EncryptionConfigCount})", "bs:Organizations",
                "SELECT TOP 15 c.id, c.CreatedUtc FROM c WHERE c.AccountId = @aid AND c.Type = 'EncryptionConfig'",
                "bs-org"));

        // Company-partitioned containers (aggregated across all companies)
        foreach (var containerName in companyContainers)
        {
            var total = acctSummary.Companies.Sum(c => c.ChildCounts.GetValueOrDefault(containerName));
            if (total > 0)
            {
                // We'll query across all company IDs — use the first company with data as example
                drillOptions.Add(($"{containerName} ({total} across {acctSummary.CompanyCount} companies)", $"bs:{containerName}",
                    $"SELECT TOP 15 c.id, c.Type, c.CompanyId, c.CreatedUtc FROM c WHERE c.CompanyId IN ({string.Join(",", acctSummary.Companies.Select((c, i) => $"@cid{i}"))})",
                    "bs-company"));
            }
        }

        var icTotal = acctSummary.Companies.Sum(c => c.InterCompanyCount);
        if (icTotal > 0)
        {
            drillOptions.Add(($"InterCompany ({icTotal})", "bs:InterCompany",
                "SELECT TOP 15 c.id, c.SourceCompanyId, c.TargetCompanyId, c.Status, c.CreatedUtc FROM c WHERE c.OrganizationId = @oid",
                "bs-ic"));
        }

        Console.WriteLine("  Containers with data:");
        for (int i = 0; i < drillOptions.Count; i++)
            Console.WriteLine($"    [{i + 1}] {drillOptions[i].Label}");
        Console.WriteLine("    [0] Back");
        Console.WriteLine();
        Console.Write($"  Choice (0-{drillOptions.Count}): ");

        var contInput = Console.ReadLine()?.Trim();
        if (!int.TryParse(contInput, out var contIdx) || contIdx == 0)
            break;
        if (contIdx < 1 || contIdx > drillOptions.Count)
            continue;

        var option = drillOptions[contIdx - 1];
        Console.WriteLine();
        Console.WriteLine($"  ── Top 15 records: {option.Label} ──");
        Console.WriteLine();

        try
        {
            List<JsonElement> records;

            if (option.PartitionSource == "daisi")
            {
                var container = daisiDatabase.GetContainer("Accounts");
                records = await QueryItems(container, option.Query, ("@aid", acctSummary.AccountId));
            }
            else if (option.PartitionSource == "bs-org")
            {
                var container = bsDatabase.GetContainer("Organizations");
                records = await QueryItems(container, option.Query, ("@aid", acctSummary.AccountId));
            }
            else if (option.PartitionSource == "bs-ic")
            {
                var container = bsDatabase.GetContainer("InterCompany");
                var firstOrgId = acctSummary.OrganizationIds.FirstOrDefault() ?? "";
                records = await QueryItems(container, option.Query, ("@oid", firstOrgId));
            }
            else // bs-company — query per company, collect up to 15 total
            {
                var realContainerName = option.Container.Replace("bs:", "");
                var container = bsDatabase.GetContainer(realContainerName);
                records = [];
                foreach (var co in acctSummary.Companies)
                {
                    if (records.Count >= 15) break;
                    var remaining = 15 - records.Count;
                    var coRecords = await QueryItems(container,
                        $"SELECT TOP {remaining} c.id, c.Type, c.CompanyId, c.CreatedUtc FROM c WHERE c.CompanyId = @cid",
                        ("@cid", co.Id));
                    records.AddRange(coRecords);
                }
            }

            if (records.Count == 0)
            {
                Console.WriteLine("    (no records found)");
            }
            else
            {
                foreach (var rec in records)
                {
                    // Print all non-null properties as a compact line
                    var props = new List<string>();
                    foreach (var prop in rec.EnumerateObject())
                    {
                        if (prop.Name == "_rid" || prop.Name == "_self" || prop.Name == "_etag"
                            || prop.Name == "_attachments" || prop.Name == "_ts")
                            continue;
                        var val = prop.Value.ValueKind switch
                        {
                            JsonValueKind.String => prop.Value.GetString(),
                            JsonValueKind.Number => prop.Value.GetRawText(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            JsonValueKind.Null => null,
                            _ => prop.Value.GetRawText().Length > 50
                                ? prop.Value.GetRawText()[..47] + "..."
                                : prop.Value.GetRawText()
                        };
                        if (val != null)
                            props.Add($"{prop.Name}={val}");
                    }
                    Console.WriteLine($"    {string.Join("  ", props)}");
                }
            }
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"    ERROR: {ex.StatusCode} — {ex.Message}");
        }

        Console.WriteLine();
    }
    Console.WriteLine();
}

// ═══════════════════════════════════════════════════
// Phase 3: CONFIRM — select what to delete
// ═══════════════════════════════════════════════════
Console.WriteLine("── Phase 3: CONFIRM ───────────────────────────────────────────");
Console.WriteLine();
var e2eAccountCount = accountSummaries.Count(s => s.IsE2eAccount);
var protectedAccountCount = accountSummaries.Count(s => !s.IsE2eAccount);
Console.WriteLine("This will delete:");
Console.WriteLine("  - daisi-booksmarts: Organizations, Companies, all child data (all accounts)");
if (e2eAccountCount > 0)
    Console.WriteLine($"  - daisi: Account + User documents ({e2eAccountCount} E2E test account(s))");
if (protectedAccountCount > 0)
    Console.WriteLine($"  - daisi: Account + Users PROTECTED for {protectedAccountCount} pre-existing account(s)");
Console.WriteLine();
Console.Write($"Type 'DELETE' to remove all {grandTotalItems} items, or anything else to abort: ");
var confirm = Console.ReadLine()?.Trim();

if (confirm != "DELETE")
{
    Console.WriteLine("Aborted. No data was modified.");
    return 0;
}

// ═══════════════════════════════════════════════════
// Phase 4: DELETE
// ═══════════════════════════════════════════════════
Console.WriteLine();
Console.WriteLine("Deleting...");
Console.WriteLine();

var deleted = 0;
var errors = 0;

foreach (var summary in accountSummaries)
{
    Console.WriteLine($"  Account: \"{summary.AccountName}\" ({summary.AccountId})");

    var orgsContainer = bsDatabase.GetContainer("Organizations");

    // Delete child data for each company
    foreach (var co in summary.Companies)
    {
        Console.Write($"    Company [{co.Id}]:");

        foreach (var containerName in companyContainers)
        {
            if (co.ChildCounts.GetValueOrDefault(containerName) == 0)
                continue;

            try
            {
                var container = bsDatabase.GetContainer(containerName);
                var items = await QueryItems(container,
                    "SELECT c.id FROM c WHERE c.CompanyId = @cid AND c.CreatedUtc >= @cutoff",
                    ("@cid", co.Id), ("@cutoff", e2eCutoffStr));

                foreach (var item in items)
                {
                    try
                    {
                        await container.DeleteItemAsync<object>(GetStr(item, "id")!, new PartitionKey(co.Id));
                        deleted++;
                    }
                    catch (CosmosException ex)
                    {
                        Console.Write($" ERR({containerName})");
                        errors++;
                    }
                }
            }
            catch (CosmosException)
            {
                errors++;
            }
        }

        // InterCompany
        if (co.InterCompanyCount > 0 && !string.IsNullOrEmpty(co.OrganizationId))
        {
            try
            {
                var icContainer = bsDatabase.GetContainer("InterCompany");
                var icItems = await QueryItems(icContainer,
                    "SELECT c.id, c.OrganizationId FROM c WHERE c.OrganizationId = @oid AND (c.SourceCompanyId = @cid OR c.TargetCompanyId = @cid) AND c.CreatedUtc >= @cutoff",
                    ("@oid", co.OrganizationId), ("@cid", co.Id), ("@cutoff", e2eCutoffStr));

                foreach (var item in icItems)
                {
                    try
                    {
                        await icContainer.DeleteItemAsync<object>(GetStr(item, "id")!, new PartitionKey(GetStr(item, "OrganizationId")!));
                        deleted++;
                    }
                    catch (CosmosException) { errors++; }
                }
            }
            catch (CosmosException) { errors++; }
        }

        // Delete company document
        try
        {
            await orgsContainer.DeleteItemAsync<object>(co.Id, new PartitionKey(summary.AccountId));
            deleted++;
            Console.Write(" company");
        }
        catch (CosmosException) { errors++; }

        Console.WriteLine(" done.");
    }

    // Delete BookSmarts users, encryption configs, divisions, organizations
    foreach (var id in summary.BookSmartsUserIds)
    {
        try { await orgsContainer.DeleteItemAsync<object>(id, new PartitionKey(summary.AccountId)); deleted++; }
        catch (CosmosException) { errors++; }
    }
    if (summary.BookSmartsUserCount > 0)
        Console.WriteLine($"    BookSmarts users: {summary.BookSmartsUserCount} deleted");

    foreach (var id in summary.EncryptionConfigIds)
    {
        try { await orgsContainer.DeleteItemAsync<object>(id, new PartitionKey(summary.AccountId)); deleted++; }
        catch (CosmosException) { errors++; }
    }
    if (summary.EncryptionConfigCount > 0)
        Console.WriteLine($"    Encryption configs: {summary.EncryptionConfigCount} deleted");

    foreach (var id in summary.DivisionIds)
    {
        try { await orgsContainer.DeleteItemAsync<object>(id, new PartitionKey(summary.AccountId)); deleted++; }
        catch (CosmosException) { errors++; }
    }
    if (summary.DivisionCount > 0)
        Console.WriteLine($"    Divisions: {summary.DivisionCount} deleted");

    foreach (var id in summary.OrganizationIds)
    {
        try { await orgsContainer.DeleteItemAsync<object>(id, new PartitionKey(summary.AccountId)); deleted++; }
        catch (CosmosException) { errors++; }
    }
    if (summary.OrganizationCount > 0)
        Console.WriteLine($"    Organizations: {summary.OrganizationCount} deleted");

    // Only delete daisi account + users for E2E test accounts (not pre-existing ones)
    if (summary.IsE2eAccount)
    {
        var allDaisiUsers = await QueryItems(daisiAccounts,
            "SELECT c.id FROM c WHERE c.AccountId = @aid AND c.type = 'User'",
            ("@aid", summary.AccountId));
        foreach (var u in allDaisiUsers)
        {
            try { await daisiAccounts.DeleteItemAsync<object>(GetStr(u, "id")!, new PartitionKey(summary.AccountId)); deleted++; }
            catch (CosmosException) { errors++; }
        }
        Console.WriteLine($"    Daisi users: {allDaisiUsers.Count} deleted");

        try
        {
            await daisiAccounts.DeleteItemAsync<object>(summary.AccountId, new PartitionKey(summary.AccountId));
            deleted++;
            Console.WriteLine($"    Daisi account document deleted.");
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"    ERROR deleting account: {ex.StatusCode}");
            errors++;
        }
    }
    else
    {
        Console.WriteLine($"    Daisi account + users: PROTECTED (pre-existing account)");
    }

    Console.WriteLine();
}

Console.WriteLine("════════════════════════════════════════════════════════════════");
Console.WriteLine($"  Done. Deleted: {deleted}  Errors: {errors}");
Console.WriteLine("════════════════════════════════════════════════════════════════");

return errors > 0 ? 2 : 0;

// ─────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────

static async Task<List<JsonElement>> QueryItems(Container container, string sql, params (string name, string value)[] parameters)
{
    var queryDef = new QueryDefinition(sql);
    foreach (var (name, value) in parameters)
        queryDef = queryDef.WithParameter(name, value);

    var results = new List<JsonElement>();
    using var iterator = container.GetItemQueryIterator<JsonElement>(queryDef);
    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        results.AddRange(response);
    }
    return results;
}

static string? GetStr(JsonElement el, string propName)
{
    if (el.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
        return prop.GetString();
    return null;
}

static DateTime? TryParseDate(JsonElement el, string propName)
{
    var s = GetStr(el, propName);
    if (s != null && DateTime.TryParse(s, out var dt))
        return dt.ToUniversalTime();
    return null;
}

static string FormatDate(DateTime? dt) => dt?.ToString("yyyy-MM-dd HH:mm") ?? "—";

class AccountSummary
{
    public string AccountId { get; set; } = "";
    public string AccountName { get; set; } = "";
    public bool IsE2eAccount { get; set; }
    public int DaisiUserCount { get; set; }
    public int E2eUserCount { get; set; }
    public int OrganizationCount { get; set; }
    public List<string> OrganizationIds { get; set; } = [];
    public int CompanyCount { get; set; }
    public List<CompanyInfo> Companies { get; set; } = [];
    public int BookSmartsUserCount { get; set; }
    public List<string> BookSmartsUserIds { get; set; } = [];
    public int EncryptionConfigCount { get; set; }
    public List<string> EncryptionConfigIds { get; set; } = [];
    public int DivisionCount { get; set; }
    public List<string> DivisionIds { get; set; } = [];
}

class CompanyInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? OrganizationId { get; set; }
    public DateTime? CreatedUtc { get; set; }
    public Dictionary<string, int> ChildCounts { get; set; } = new();
    public int InterCompanyCount { get; set; }
}
