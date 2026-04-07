namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for Chart of Accounts, Journal Entries, and Payments.
/// </summary>
public class AccountingTests : AuthenticatedTestBase
{
    // --- Chart of Accounts ---

    [Fact]
    public async Task ChartOfAccounts_NavigateAndLoad()
    {
        await ClickNavItem("Chart of Accounts");
        await AssertHeading("Chart of Accounts");
        await TakeScreenshot("chart_of_accounts");
    }

    [Fact]
    public async Task ChartOfAccounts_PageLoadsSuccessfully()
    {
        await NavigateTo("/chart-of-accounts");
        await WaitForBlazor();

        // Page should load without error — accounts may or may not exist
        // depending on whether a company has been created
        await TakeScreenshot("chart_of_accounts_loaded");
    }

    // --- Journal Entries ---

    [Fact]
    public async Task JournalEntries_NavigateAndLoad()
    {
        await ClickNavItem("Journal Entries");
        await AssertHeading("Journal Entries");
        await TakeScreenshot("journal_entries_list");
    }

    [Fact]
    public async Task JournalEntries_CreateNew_FormLoads()
    {
        await NavigateTo("/journal-entries/new");
        await WaitForBlazor();

        // Page should load — form visibility depends on user role/permissions
        await TakeScreenshot("journal_entry_new");
    }

    [Fact]
    public async Task JournalEntries_CreateBalancedEntry()
    {
        await NavigateTo("/journal-entries/new");
        await WaitForBlazor();

        // This test creates a simple balanced journal entry:
        // Debit Cash, Credit Revenue
        // Exact form interactions depend on the UI; this is a scaffold.
        await TakeScreenshot("journal_entry_create_start");

        // TODO: Fill debit/credit lines and submit
        // await Page.Locator("[data-testid='add-line']").ClickAsync();
        // ... fill account, amount, etc.
    }

    [Fact]
    public async Task JournalEntries_ViewExisting()
    {
        await NavigateTo("/journal-entries");
        await WaitForBlazor();

        // Click the first journal entry in the list (if any exist)
        var firstRow = Page.Locator("table tbody tr, .mud-table-body tr").First;
        if (await firstRow.IsVisibleAsync())
        {
            await firstRow.ClickAsync();
            await WaitForBlazor();
            await TakeScreenshot("journal_entry_detail");
        }
    }

    // --- Payments ---

    [Fact]
    public async Task Payments_NavigateAndLoad()
    {
        await NavigateTo("/payments");
        await WaitForBlazor();
        await TakeScreenshot("payments_list");
    }
}
