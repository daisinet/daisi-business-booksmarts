namespace BookSmarts.E2E.Tests.Scenarios;

/// <summary>
/// Scenario 1: Freelance Marketing Consultant
///
/// Models a solo operator using BookSmarts. Creates a company, adds a customer,
/// invoices them, receives payment, creates a vendor bill, pays it, and reviews reports.
///
/// Uses AuthenticatedTestBase with the configured test user (who is provisioned in BookSmarts).
/// Each test is self-contained — helpers ensure prerequisites exist before each step.
/// </summary>
public class FreelanceConsultantTests : ScenarioTestBase
{
    private static readonly string CompanyName = $"Sarah Chen Consulting LLC ({RunId})";
    private const string CustomerName = "Oakwood Brewery";
    private const string VendorName = "Office Depot";

    // --- Step 1: Ensure Organization & Create Company ---

    [Fact]
    public async Task Step1_EnsureOrgAndCreateCompany()
    {
        await EnsureOrganization($"E2E Test Organization ({RunId})");

        await NavigateTo("/settings/companies");
        await WaitForBlazor();

        await TakeScreenshot("s1_companies_page");

        var addBtn = Page.Locator("button:text('Add Company'), button:text('Add a Company')").First;
        await addBtn.ClickAsync();
        await WaitForBlazor();

        await TakeScreenshot("s1_add_company_form");

        var nameInput = Page.Locator("input[placeholder*='West Coast']");
        await nameInput.FillAsync(CompanyName);
        await nameInput.PressAsync("Tab");

        var seedCoa = Page.Locator("#seedCoaCheck");
        if (!await seedCoa.IsCheckedAsync())
            await seedCoa.CheckAsync();

        await Page.Locator("button:text('Save')").ClickAsync();
        await WaitForBlazor();

        await TakeScreenshot("s1_after_save");

        await AssertCompanyInHeader(CompanyName);

        await TakeScreenshot("s1_company_created");
    }

    // --- Step 2: Add Customer ---

    [Fact]
    public async Task Step2_AddCustomer()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/customers");
        await WaitForBlazor();

        await Page.Locator("button:text('Add Customer'), button:text('Add a Customer')").First.ClickAsync();
        await WaitForBlazor();

        await Page.Locator("input[placeholder*='Company or individual']").FillAsync(CustomerName);
        await FillFormField("Contact Person", "Mike Johnson");
        await FillFormField("Email", "mike@oakwoodbrewery.com");
        await FillFormField("Phone", "503-555-0199");

        await Page.Locator("button:text('Save')").ClickAsync();
        await WaitForBlazor();

        await Assertions.Expect(Page.Locator("main").GetByText(CustomerName).First).ToBeVisibleAsync(
            new() { Timeout = 10000 });

        await TakeScreenshot("s1_customer_added");
    }

    // --- Step 3: Create Invoice ---

    [Fact]
    public async Task Step3_CreateInvoice()
    {
        await EnsureCompany(CompanyName);
        await EnsureCustomer(CustomerName, "Mike Johnson", "mike@oakwoodbrewery.com");

        await CreateInvoiceForCustomer(CustomerName, "Brand Strategy & Logo Design", 1, 3500);

        await TakeScreenshot("s1_invoice_saved");
    }

    // --- Step 4: Receive Payment ---

    [Fact]
    public async Task Step4_ReceivePayment()
    {
        await EnsureCompany(CompanyName);
        await EnsureCustomer(CustomerName, "Mike Johnson", "mike@oakwoodbrewery.com");
        await CreateInvoiceDraft("Brand Strategy", 3500);

        await ReceivePayment(CustomerName, 3500);

        await TakeScreenshot("s1_payment_received");
    }

    // --- Step 5: Add Vendor ---

    [Fact]
    public async Task Step5_AddVendor()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/vendors");
        await WaitForBlazor();

        await Page.Locator("button:text('Add Vendor'), button:text('Add a Vendor')").First.ClickAsync();
        await WaitForBlazor();

        await Page.Locator("input[placeholder*='Company or individual']").FillAsync(VendorName);
        await FillFormField("Email", "orders@officedepot.com");

        await Page.Locator("button:text('Save')").ClickAsync();
        await WaitForBlazor();

        await Assertions.Expect(Page.Locator("main").GetByText(VendorName).First).ToBeVisibleAsync(
            new() { Timeout = 10000 });

        await TakeScreenshot("s1_vendor_added");
    }

    // --- Step 6: Create Bill ---

    [Fact]
    public async Task Step6_CreateBill()
    {
        await EnsureCompany(CompanyName);
        await EnsureVendor(VendorName, "orders@officedepot.com");

        await CreateBillForVendor(VendorName, "Office Supplies - Printer Paper & Toner", 1, 247.50m);

        await TakeScreenshot("s1_bill_saved");
    }

    // --- Step 7: Make Payment ---

    [Fact]
    public async Task Step7_MakePayment()
    {
        await EnsureCompany(CompanyName);
        await EnsureVendor(VendorName, "orders@officedepot.com");
        await CreateBillDraft("Office Supplies", 247.50m);

        await MakePayment(VendorName, 247.50m);

        await TakeScreenshot("s1_payment_made");
    }

    // --- Step 8: Review Reports ---

    [Fact]
    public async Task Step8_ReviewReports()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/reports/balance-sheet");
        await WaitForBlazor();
        await AssertHeading("Balance Sheet");
        await TakeScreenshot("s1_balance_sheet");

        await NavigateTo("/reports/income-statement");
        await WaitForBlazor();
        await AssertHeading("Income Statement");
        await TakeScreenshot("s1_income_statement");

        await NavigateTo("/trial-balance");
        await WaitForBlazor();
        await AssertHeading("Trial Balance");
        await TakeScreenshot("s1_trial_balance");
    }
}
