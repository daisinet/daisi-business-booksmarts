namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for Budget creation, editing, and budget vs actual reporting.
/// </summary>
public class BudgetingTests : AuthenticatedTestBase
{
    [Fact]
    public async Task Budgets_NavigateAndLoad()
    {
        await ClickNavItem("Budgets");
        await AssertHeading("Budgets");
        await TakeScreenshot("budgets_list");
    }

    [Fact]
    public async Task Budgets_CreateNew_FormLoads()
    {
        await NavigateTo("/budgets/new");
        await WaitForBlazor();
        await TakeScreenshot("budget_create_form");
    }

    [Fact]
    public async Task Budgets_CreateAndEdit()
    {
        await NavigateTo("/budgets/new");
        await WaitForBlazor();

        // TODO: Fill budget name, period, line items, and save
        // Then navigate to edit and verify values persisted
        await TakeScreenshot("budget_create_start");
    }
}
