namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for Invoices, Customers, and receive-payment workflows.
/// </summary>
public class ReceivablesTests : AuthenticatedTestBase
{
    [Fact]
    public async Task Invoices_NavigateAndLoad()
    {
        await ClickNavItem("Invoices");
        await AssertHeading("Invoices");
        await TakeScreenshot("invoices_list");
    }

    [Fact]
    public async Task Invoices_CreateNew_FormLoads()
    {
        await NavigateTo("/invoices/new");
        await WaitForBlazor();
        await TakeScreenshot("invoice_create_form");
    }

    [Fact]
    public async Task Invoices_CreateAndSend()
    {
        await NavigateTo("/invoices/new");
        await WaitForBlazor();

        // TODO: Fill customer, line items, due date, and submit
        // Verify the invoice status is Draft, then send it
        await TakeScreenshot("invoice_create_start");
    }

    [Fact]
    public async Task Customers_NavigateAndLoad()
    {
        await ClickNavItem("Customers");
        await AssertHeading("Customers");
        await TakeScreenshot("customers_list");
    }

    [Fact]
    public async Task ReceivePayment_NavigateAndLoad()
    {
        await NavigateTo("/invoices/receive-payment");
        await WaitForBlazor();
        await TakeScreenshot("receive_payment");
    }
}
