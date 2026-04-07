namespace BookSmarts.E2E.Tests.Scenarios;

/// <summary>
/// Scenario 2: Small Retail Shop
///
/// Models a small retail business with multiple customers and vendors,
/// journal entries, chart of accounts management, and budget tracking.
/// Exercises features beyond basic invoicing: COA customization, journal entries,
/// budgets, and aging reports.
/// </summary>
public class RetailShopTests : ScenarioTestBase
{
    private static readonly string CompanyName = $"Maple Street Books ({RunId})";

    // --- Step 1: Setup Company ---

    [Fact]
    public async Task Step1_SetupCompany()
    {
        await EnsureCompany(CompanyName);

        // Verify we're on a page with the sidebar (company was set up)
        await Assertions.Expect(Page.Locator(".bs-sidebar").First).ToBeVisibleAsync(
            new() { Timeout = 10000 });

        await TakeScreenshot("s2_company_ready");
    }

    // --- Step 2: Review Chart of Accounts ---

    [Fact]
    public async Task Step2_ReviewChartOfAccounts()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/chart-of-accounts");
        await WaitForBlazor();

        await AssertHeading("Chart of Accounts");

        // Verify default accounts exist
        await Assertions.Expect(Page.Locator("table, .bs-table").First).ToBeVisibleAsync(
            new() { Timeout = 10000 });

        await TakeScreenshot("s2_chart_of_accounts");
    }

    // --- Step 3: Add Multiple Customers ---

    [Fact]
    public async Task Step3_AddCustomers()
    {
        await EnsureCompany(CompanyName);

        await EnsureCustomer("Portland Book Club", "Lisa Park", "lisa@pdxbookclub.com");
        await TakeScreenshot("s2_customer1_added");

        await EnsureCustomer("River City School District", "Tom Nguyen", "tom.nguyen@rcsd.edu");
        await TakeScreenshot("s2_customer2_added");

        await EnsureCustomer("Coffee & Pages Cafe", "Emma Wilson", "emma@coffeepages.com");
        await TakeScreenshot("s2_customer3_added");
    }

    // --- Step 4: Add Multiple Vendors ---

    [Fact]
    public async Task Step4_AddVendors()
    {
        await EnsureCompany(CompanyName);

        await EnsureVendor("Penguin Random House", "orders@penguinrandomhouse.com");
        await TakeScreenshot("s2_vendor1_added");

        await EnsureVendor("Ingram Book Company", "wholesale@ingrambook.com");
        await TakeScreenshot("s2_vendor2_added");
    }

    // --- Step 5: Create Multiple Invoices ---

    [Fact]
    public async Task Step5_CreateInvoices()
    {
        await EnsureCompany(CompanyName);
        await EnsureCustomer("Portland Book Club", "Lisa Park", "lisa@pdxbookclub.com");
        await EnsureCustomer("River City School District", "Tom Nguyen", "tom.nguyen@rcsd.edu");

        await CreateInvoiceForCustomer("Portland Book Club", "Book order - Fiction collection", 25, 18.99m);
        await TakeScreenshot("s2_invoice1_saved");

        await CreateInvoiceForCustomer("River City School District", "Classroom library set - Grade 3-5", 50, 12.50m);
        await TakeScreenshot("s2_invoice2_saved");
    }

    // --- Step 6: Create Vendor Bills ---

    [Fact]
    public async Task Step6_CreateBills()
    {
        await EnsureCompany(CompanyName);
        await EnsureVendor("Penguin Random House", "orders@penguinrandomhouse.com");
        await EnsureVendor("Ingram Book Company", "wholesale@ingrambook.com");

        await CreateBillForVendor("Penguin Random House", "Wholesale books - Q1 order", 100, 8.50m);
        await TakeScreenshot("s2_bill1_saved");

        await CreateBillForVendor("Ingram Book Company", "Wholesale books - Bestsellers", 75, 9.25m);
        await TakeScreenshot("s2_bill2_saved");
    }

    // --- Step 7: Receive Payments ---

    [Fact]
    public async Task Step7_ReceivePayments()
    {
        await EnsureCompany(CompanyName);
        await EnsureCustomer("Portland Book Club", "Lisa Park", "lisa@pdxbookclub.com");
        await CreateInvoiceDraft("Book order", 474.75m);

        await ReceivePayment("Portland Book Club", 474.75m);
        await TakeScreenshot("s2_payment_received");
    }

    // --- Step 8: Make Vendor Payments ---

    [Fact]
    public async Task Step8_MakePayments()
    {
        await EnsureCompany(CompanyName);
        await EnsureVendor("Penguin Random House", "orders@penguinrandomhouse.com");
        await CreateBillDraft("Wholesale books", 850m);

        await MakePayment("Penguin Random House", 850m);
        await TakeScreenshot("s2_vendor_payment_made");
    }

    // --- Step 9: Create Journal Entry ---

    [Fact]
    public async Task Step9_CreateJournalEntry()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/journal-entries/new");
        await WaitForBlazor();

        await TakeScreenshot("s2_journal_entry_form");

        // Fill memo/description
        var memoInput = Page.Locator("input[placeholder*='memo'], input[placeholder*='description'], textarea").First;
        if (await memoInput.IsVisibleAsync())
            await memoInput.FillAsync("Monthly rent expense - March 2026");

        // Fill date if available
        var dateInput = Page.Locator("input[type='date']").First;
        if (await dateInput.IsVisibleAsync())
            await dateInput.FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));

        // Fill debit line
        var descInputs = Page.Locator("tbody input[type='text']");
        if (await descInputs.CountAsync() > 0)
            await descInputs.First.FillAsync("Office rent");

        var numberInputs = Page.Locator("tbody input[type='number']");
        if (await numberInputs.CountAsync() >= 2)
        {
            await numberInputs.Nth(0).FillAsync("1500");
            // Credit line — often the second row's debit column or a separate credit column
        }

        await SelectFirstAvailableAccount("tbody select");

        await TakeScreenshot("s2_journal_entry_filled");

        // Save
        var saveBtn = Page.Locator("button:text('Save'), button:text('Save as Draft'), button:text('Post')").First;
        await saveBtn.ClickAsync();
        await WaitForBlazor();

        await TakeScreenshot("s2_journal_entry_saved");
    }

    // --- Step 10: Review Aging Reports ---

    [Fact]
    public async Task Step10_ReviewAgingReports()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/reports/ar-aging");
        await WaitForBlazor();
        await AssertHeading("AR Aging");
        await TakeScreenshot("s2_ar_aging");

        await NavigateTo("/reports/ap-aging");
        await WaitForBlazor();
        await AssertHeading("AP Aging");
        await TakeScreenshot("s2_ap_aging");
    }

    // --- Step 11: Review Financial Reports ---

    [Fact]
    public async Task Step11_ReviewReports()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/reports/balance-sheet");
        await WaitForBlazor();
        await AssertHeading("Balance Sheet");
        await TakeScreenshot("s2_balance_sheet");

        await NavigateTo("/reports/income-statement");
        await WaitForBlazor();
        await AssertHeading("Income Statement");
        await TakeScreenshot("s2_income_statement");

        await NavigateTo("/reports/cash-flow");
        await WaitForBlazor();
        await AssertHeading("Cash Flow");
        await TakeScreenshot("s2_cash_flow");

        await NavigateTo("/trial-balance");
        await WaitForBlazor();
        await AssertHeading("Trial Balance");
        await TakeScreenshot("s2_trial_balance");
    }
}
