namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for Bank Connections, Transactions, and Categorization Rules.
/// </summary>
public class BankingTests : AuthenticatedTestBase
{
    [Fact]
    public async Task BankConnections_NavigateAndLoad()
    {
        await ClickNavItem("Bank Connections");
        await AssertHeading("Bank Connections");
        await TakeScreenshot("bank_connections");
    }

    [Fact]
    public async Task BankTransactions_NavigateAndLoad()
    {
        await ClickNavItem("Bank Transactions");
        await AssertHeading("Bank Transactions");
        await TakeScreenshot("bank_transactions");
    }

    [Fact]
    public async Task CategorizationRules_NavigateAndLoad()
    {
        await NavigateTo("/banking/rules");
        await WaitForBlazor();
        await TakeScreenshot("categorization_rules");
    }

    [Fact]
    public async Task BankTransactions_FilterAndCategorize()
    {
        await NavigateTo("/banking/transactions");
        await WaitForBlazor();

        // TODO: Filter to uncategorized transactions, select one, categorize it
        await TakeScreenshot("bank_transactions_filter");
    }
}
