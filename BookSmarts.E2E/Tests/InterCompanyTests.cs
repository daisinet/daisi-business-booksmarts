namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for Inter-Company transactions (requires multi-company setup).
/// </summary>
public class InterCompanyTests : AuthenticatedTestBase
{
    [Fact]
    public async Task InterCompany_NavigateAndLoad()
    {
        await NavigateTo("/inter-company");
        await WaitForBlazor();
        await TakeScreenshot("inter_company_list");
    }

    [Fact]
    public async Task InterCompany_CreateNew_FormLoads()
    {
        await NavigateTo("/inter-company/new");
        await WaitForBlazor();
        await TakeScreenshot("inter_company_create");
    }
}
