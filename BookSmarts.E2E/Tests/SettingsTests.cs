namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for Settings pages: Organization Setup, Companies, Divisions,
/// Fiscal Periods, Team Management, and Audit Log.
/// </summary>
public class SettingsTests : AuthenticatedTestBase
{
    [Fact]
    public async Task OrganizationSetup_NavigateAndLoad()
    {
        await NavigateTo("/settings/organization");
        await WaitForBlazor();
        await TakeScreenshot("organization_setup");
    }

    [Fact]
    public async Task Companies_NavigateAndLoad()
    {
        await NavigateTo("/settings/companies");
        await WaitForBlazor();
        await TakeScreenshot("companies_list");
    }

    [Fact]
    public async Task Divisions_NavigateAndLoad()
    {
        await NavigateTo("/settings/divisions");
        await WaitForBlazor();
        await TakeScreenshot("divisions_list");
    }

    [Fact]
    public async Task FiscalPeriods_NavigateAndLoad()
    {
        await NavigateTo("/settings/fiscal-periods");
        await WaitForBlazor();
        await TakeScreenshot("fiscal_periods");
    }

    [Fact]
    public async Task TeamManagement_NavigateAndLoad()
    {
        await NavigateTo("/settings/team");
        await WaitForBlazor();
        await TakeScreenshot("team_management");
    }

    [Fact]
    public async Task AuditLog_NavigateAndLoad()
    {
        await NavigateTo("/settings/audit-log");
        await WaitForBlazor();
        await TakeScreenshot("audit_log");
    }
}
