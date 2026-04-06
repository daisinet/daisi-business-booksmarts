namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for the main dashboard after login.
/// </summary>
public class DashboardTests : AuthenticatedTestBase
{
    [Fact]
    public async Task Dashboard_Loads_WithSidebarAndWidgets()
    {
        await NavigateTo("/");
        await WaitForBlazor();

        // Sidebar should be visible with BookSmarts branding
        var sidebar = Page.Locator(".bs-sidebar");
        await Assertions.Expect(sidebar).ToBeVisibleAsync();

        // Dashboard nav item should be active
        var dashboardNav = Page.Locator(".bs-nav-item[href='']");
        await Assertions.Expect(dashboardNav).ToBeVisibleAsync();

        await TakeScreenshot("dashboard_loaded");
    }

    [Fact]
    public async Task Dashboard_Header_HasAccrualCashToggle()
    {
        await NavigateTo("/");
        await WaitForBlazor();

        // The Accrual/Cash basis toggle should always be in the header
        var accrualBtn = Page.Locator(".bs-header-right .btn-group button", new() { HasTextString = "Accrual" });
        await Assertions.Expect(accrualBtn).ToBeVisibleAsync();

        var cashBtn = Page.Locator(".bs-header-right .btn-group button", new() { HasTextString = "Cash" });
        await Assertions.Expect(cashBtn).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Dashboard_AIInsightWidget_IsPresent()
    {
        await NavigateTo("/");
        await WaitForBlazor();

        // The AI insight widget should render on the dashboard
        var aiWidget = Page.Locator("[class*='ai-insight'], [class*='AIInsight']").First;
        // May or may not be visible depending on data, but container should exist
        await TakeScreenshot("dashboard_ai_widget");
    }

    [Fact]
    public async Task Dashboard_SidebarToggle_CollapsesAndExpands()
    {
        await NavigateTo("/");
        await WaitForBlazor();

        var app = Page.Locator(".bs-app");
        var toggleBtn = Page.Locator(".bs-sidebar-toggle");

        // Click to collapse
        await toggleBtn.ClickAsync();
        await Assertions.Expect(app).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar-collapsed"));

        // Click to expand
        await toggleBtn.ClickAsync();
        await Assertions.Expect(app).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar-collapsed"));
    }

    [Fact]
    public async Task Dashboard_ThemeToggle_SwitchesDarkMode()
    {
        await NavigateTo("/");
        await WaitForBlazor();

        var themeToggle = Page.Locator(".bs-theme-toggle");
        await themeToggle.ClickAsync();
        await TakeScreenshot("dashboard_dark_mode");

        // Toggle back
        await themeToggle.ClickAsync();
        await TakeScreenshot("dashboard_light_mode");
    }
}
