namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for Bills, Vendors, and bill payment workflows.
/// </summary>
public class PayablesTests : AuthenticatedTestBase
{
    [Fact]
    public async Task Bills_NavigateAndLoad()
    {
        await ClickNavItem("Bills");
        await AssertHeading("Bills");
        await TakeScreenshot("bills_list");
    }

    [Fact]
    public async Task Bills_CreateNew_FormLoads()
    {
        await NavigateTo("/bills/new");
        await WaitForBlazor();
        await TakeScreenshot("bill_create_form");
    }

    [Fact]
    public async Task Bills_CreateAndView()
    {
        await NavigateTo("/bills/new");
        await WaitForBlazor();

        // TODO: Fill vendor, date, amount, line items, and submit
        // Then verify the bill appears in the list and can be viewed
        await TakeScreenshot("bill_create_start");
    }

    [Fact]
    public async Task Vendors_NavigateAndLoad()
    {
        await ClickNavItem("Vendors");
        await AssertHeading("Vendors");
        await TakeScreenshot("vendors_list");
    }

    [Fact]
    public async Task MakePayment_NavigateAndLoad()
    {
        await NavigateTo("/bills/pay");
        await WaitForBlazor();
        await TakeScreenshot("make_payment");
    }
}
