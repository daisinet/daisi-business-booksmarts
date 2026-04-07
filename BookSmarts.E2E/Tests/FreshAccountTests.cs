namespace BookSmarts.E2E.Tests;

/// <summary>
/// Verifies that the FreshAccountTestBase works — creates a new account
/// and lands on the BookSmarts dashboard ready for setup.
/// </summary>
public class FreshAccountTests : FreshAccountTestBase
{
    protected override string ScenarioOwnerName => "Fresh Account Owner";

    [Fact]
    public async Task FreshAccount_LandsOnDashboard_WithWelcomeMessage()
    {
        await NavigateTo("/");
        await WaitForBlazor();

        // New account should see the welcome/setup prompt
        var welcome = Page.GetByText("Welcome to BookSmarts");
        await Assertions.Expect(welcome).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Sidebar should be visible
        var sidebar = Page.Locator(".bs-sidebar");
        await Assertions.Expect(sidebar).ToBeVisibleAsync();

        await TakeScreenshot("fresh_account_dashboard");
    }

    [Fact]
    public async Task FreshAccount_CanNavigateToSettings()
    {
        await NavigateTo("/settings/organization");
        await WaitForBlazor();
        await TakeScreenshot("fresh_account_settings");
    }
}
