namespace BookSmarts.E2E.Tests.Scenarios;

/// <summary>
/// Scenario 5: E-Commerce Business
///
/// Models an online store with high-volume transactions, banking features,
/// categorization rules, journal entries, AI projections, and fiscal periods.
/// Exercises pages not covered by other scenarios: banking, categorization rules,
/// journal entries list, fiscal periods, and AI projections.
/// </summary>
public class ECommerceTests : ScenarioTestBase
{
    private static readonly string CompanyName = $"PixelCraft Studios ({RunId})";

    // --- Step 1: Setup Company ---

    [Fact]
    public async Task Step1_SetupCompany()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await TakeScreenshot("s5_company_ready");
    }

    // --- Step 2: Add Customers ---

    [Fact]
    public async Task Step2_AddCustomers()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await EnsureCustomer("Amazon Marketplace", "Seller Central", "seller@amazon.com");
        await TakeScreenshot("s5_customer1_added");

        await EnsureCustomer("Etsy Storefront", "Shop Manager", "shop@etsy.com");
        await TakeScreenshot("s5_customer2_added");

        await EnsureCustomer("Direct Website Sales", "Web Orders", "orders@pixelcraft.io");
        await TakeScreenshot("s5_customer3_added");
    }

    // --- Step 3: Add Vendors ---

    [Fact]
    public async Task Step3_AddVendors()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await EnsureVendor("Printful", "billing@printful.com");
        await TakeScreenshot("s5_vendor1_added");

        await EnsureVendor("Shopify", "billing@shopify.com");
        await TakeScreenshot("s5_vendor2_added");

        await EnsureVendor("UPS Shipping", "invoices@ups.com");
        await TakeScreenshot("s5_vendor3_added");
    }

    // --- Step 4: Create High-Volume Invoices ---

    [Fact]
    public async Task Step4_CreateInvoices()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);
        await EnsureCustomer("Amazon Marketplace", "Seller Central", "seller@amazon.com");
        await EnsureCustomer("Direct Website Sales", "Web Orders", "orders@pixelcraft.io");

        await CreateInvoiceForCustomer("Amazon", "Custom phone cases - March batch", 200, 15.99m);
        await TakeScreenshot("s5_invoice1_saved");

        await CreateInvoiceForCustomer("Direct Website", "Custom art prints - March orders", 75, 29.99m);
        await TakeScreenshot("s5_invoice2_saved");
    }

    // --- Step 5: Create Vendor Bills ---

    [Fact]
    public async Task Step5_CreateBills()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);
        await EnsureVendor("Printful", "billing@printful.com");
        await EnsureVendor("Shopify", "billing@shopify.com");
        await EnsureVendor("UPS Shipping", "invoices@ups.com");

        await CreateBillForVendor("Printful", "Print-on-demand fulfillment - March", 275, 8.50m);
        await TakeScreenshot("s5_bill1_saved");

        await CreateBillForVendor("Shopify", "Monthly subscription + transaction fees", 1, 299.00m);
        await TakeScreenshot("s5_bill2_saved");

        await CreateBillForVendor("UPS", "Shipping charges - March", 275, 4.75m);
        await TakeScreenshot("s5_bill3_saved");
    }

    // --- Step 6: Receive Payments ---

    [Fact]
    public async Task Step6_ReceivePayments()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);
        await EnsureCustomer("Amazon Marketplace", "Seller Central", "seller@amazon.com");
        await CreateInvoiceDraft("Amazon sales", 3198m);

        await ReceivePayment("Amazon", 3198m);
        await TakeScreenshot("s5_payment_received");
    }

    // --- Step 7: Make Vendor Payments ---

    [Fact]
    public async Task Step7_MakePayments()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);
        await EnsureVendor("Printful", "billing@printful.com");
        await CreateBillDraft("Fulfillment charges", 2337.50m);

        await MakePayment("Printful", 2337.50m);
        await TakeScreenshot("s5_vendor_payment_made");
    }

    // --- Step 8: View Journal Entries List ---

    [Fact]
    public async Task Step8_ViewJournalEntries()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/journal-entries");
        await WaitForBlazor();

        await TakeScreenshot("s5_journal_entries_list");
    }

    // --- Step 9: Create Journal Entry ---

    [Fact]
    public async Task Step9_CreateJournalEntry()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/journal-entries/new");
        await WaitForBlazor();

        await TakeScreenshot("s5_journal_entry_form");

        // Fill memo
        var memoInput = Page.Locator("input[placeholder*='memo'], input[placeholder*='description'], textarea").First;
        if (await memoInput.IsVisibleAsync())
            await memoInput.FillAsync("Marketplace fee adjustment - March 2026");

        // Fill date
        var dateInput = Page.Locator("input[type='date']").First;
        if (await dateInput.IsVisibleAsync())
            await dateInput.FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));

        // Fill line items
        var descInputs = Page.Locator("tbody input[type='text']");
        if (await descInputs.CountAsync() > 0)
            await descInputs.First.FillAsync("Amazon referral fee adjustment");

        var numberInputs = Page.Locator("tbody input[type='number']");
        if (await numberInputs.CountAsync() >= 1)
            await numberInputs.First.FillAsync("125.00");

        await SelectFirstAvailableAccount("tbody select");

        var saveBtn = Page.Locator("button:text('Save'), button:text('Save as Draft'), button:text('Post')").First;
        await saveBtn.ClickAsync();
        await WaitForBlazor();

        await TakeScreenshot("s5_journal_entry_saved");
    }

    // --- Step 10: View Bank Connections ---

    [Fact]
    public async Task Step10_ViewBankConnections()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/banking/connections");
        await WaitForBlazor();

        await TakeScreenshot("s5_bank_connections");
    }

    // --- Step 11: View Bank Transactions ---

    [Fact]
    public async Task Step11_ViewBankTransactions()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/banking/transactions");
        await WaitForBlazor();

        await TakeScreenshot("s5_bank_transactions");
    }

    // --- Step 12: View Categorization Rules ---

    [Fact]
    public async Task Step12_ViewCategorizationRules()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/banking/rules");
        await WaitForBlazor();

        await TakeScreenshot("s5_categorization_rules");
    }

    // --- Step 13: View Fiscal Periods ---

    [Fact]
    public async Task Step13_ViewFiscalPeriods()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/settings/fiscal-periods");
        await WaitForBlazor();

        await TakeScreenshot("s5_fiscal_periods");
    }

    // --- Step 14: View AI Projections ---

    [Fact]
    public async Task Step14_ViewProjections()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/ai/projections");
        await WaitForBlazor();

        await TakeScreenshot("s5_ai_projections");
    }

    // --- Step 15: Full Financial Review ---

    [Fact]
    public async Task Step15_FinancialReview()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/reports/balance-sheet");
        await WaitForBlazor();
        await AssertHeading("Balance Sheet");
        await TakeScreenshot("s5_balance_sheet");

        await NavigateTo("/reports/income-statement");
        await WaitForBlazor();
        await AssertHeading("Income Statement");
        await TakeScreenshot("s5_income_statement");

        await NavigateTo("/reports/cash-flow");
        await WaitForBlazor();
        await AssertHeading("Cash Flow");
        await TakeScreenshot("s5_cash_flow");

        await NavigateTo("/trial-balance");
        await WaitForBlazor();
        await AssertHeading("Trial Balance");
        await TakeScreenshot("s5_trial_balance");

        await NavigateTo("/reports/ar-aging");
        await WaitForBlazor();
        await AssertHeading("AR Aging");
        await TakeScreenshot("s5_ar_aging");

        await NavigateTo("/reports/ap-aging");
        await WaitForBlazor();
        await AssertHeading("AP Aging");
        await TakeScreenshot("s5_ap_aging");
    }
}
