namespace BookSmarts.E2E.Tests.Scenarios;

/// <summary>
/// Scenario 4: Multi-Entity Holding Company
///
/// Models a holding company with two subsidiaries. Exercises multi-company
/// features: inter-company transactions, consolidated financial reports,
/// and company switching. Requires 2+ companies to unlock consolidated views.
/// </summary>
public class HoldingCompanyTests : ScenarioTestBase
{
    private static readonly string Company1 = $"Apex Holdings Corp ({RunId})";
    private static readonly string Company2 = $"Apex Manufacturing LLC ({RunId})";

    // --- Step 1: Create Two Companies ---

    [Fact]
    public async Task Step1_CreateCompanies()
    {
        await EnsureCompany(Company1);
        await EnsureCompany(Company2);

        await TakeScreenshot("s4_companies_created");
    }

    // --- Step 2: Setup Customers & Vendors for Company 1 ---

    [Fact]
    public async Task Step2_SetupCompany1Entities()
    {
        await EnsureCompany(Company1);
        await SelectCompany(Company1);

        await EnsureCustomer("National Distributors Inc", "Rachel Kim", "rachel@natdist.com");
        await TakeScreenshot("s4_c1_customer_added");

        await EnsureVendor("Raw Materials Supply Co", "orders@rawmatsupply.com");
        await TakeScreenshot("s4_c1_vendor_added");
    }

    // --- Step 3: Setup Customers & Vendors for Company 2 ---

    [Fact]
    public async Task Step3_SetupCompany2Entities()
    {
        await EnsureCompany(Company2);
        await SelectCompany(Company2);

        await EnsureCustomer("Apex Holdings Corp", "Internal", "internal@apexholdings.com");
        await TakeScreenshot("s4_c2_customer_added");

        await EnsureVendor("Industrial Equipment Ltd", "sales@industrialequip.com");
        await TakeScreenshot("s4_c2_vendor_added");
    }

    // --- Step 4: Create Invoice in Company 1 ---

    [Fact]
    public async Task Step4_InvoiceFromCompany1()
    {
        await EnsureCompany(Company1);
        await SelectCompany(Company1);
        await EnsureCustomer("National Distributors Inc", "Rachel Kim", "rachel@natdist.com");

        await CreateInvoiceForCustomer("National Distributors", "Wholesale product shipment - Q1", 500, 45.00m);
        await TakeScreenshot("s4_c1_invoice_created");
    }

    // --- Step 5: Create Bill in Company 2 ---

    [Fact]
    public async Task Step5_BillInCompany2()
    {
        await EnsureCompany(Company2);
        await SelectCompany(Company2);
        await EnsureVendor("Industrial Equipment Ltd", "sales@industrialequip.com");

        await CreateBillForVendor("Industrial Equipment", "CNC Machine lease payment - March", 1, 8500.00m);
        await TakeScreenshot("s4_c2_bill_created");
    }

    // --- Step 6: Receive Payment in Company 1 ---

    [Fact]
    public async Task Step6_ReceivePaymentCompany1()
    {
        await EnsureCompany(Company1);
        await SelectCompany(Company1);
        await EnsureCustomer("National Distributors Inc", "Rachel Kim", "rachel@natdist.com");
        await CreateInvoiceDraft("Wholesale shipment", 22500m);

        await ReceivePayment("National Distributors", 22500m);
        await TakeScreenshot("s4_c1_payment_received");
    }

    // --- Step 7: Make Payment in Company 2 ---

    [Fact]
    public async Task Step7_MakePaymentCompany2()
    {
        await EnsureCompany(Company2);
        await SelectCompany(Company2);
        await EnsureVendor("Industrial Equipment Ltd", "sales@industrialequip.com");
        await CreateBillDraft("Equipment lease", 8500m);

        await MakePayment("Industrial Equipment", 8500m);
        await TakeScreenshot("s4_c2_payment_made");
    }

    // --- Step 8: View Inter-Company Page ---

    [Fact]
    public async Task Step8_ViewInterCompany()
    {
        await EnsureCompany(Company1);
        await EnsureCompany(Company2);

        await NavigateTo("/inter-company");
        await WaitForBlazor();

        await TakeScreenshot("s4_inter_company_page");
    }

    // --- Step 9: Create Inter-Company Transaction ---

    [Fact]
    public async Task Step9_CreateInterCompanyTransaction()
    {
        await EnsureCompany(Company1);
        await EnsureCompany(Company2);

        await NavigateTo("/inter-company/new");
        await WaitForBlazor();

        await TakeScreenshot("s4_inter_company_form");

        // Fill the form — select source/target companies and amount
        // Use header row selects (not tbody account selects which have 100+ options)
        var companySelects = Page.Locator("div.row select.form-select");
        if (await companySelects.CountAsync() >= 2)
        {
            await WaitForOptionContaining(companySelects.First, Company1);
            await SelectFirstOptionContaining(companySelects.First, Company1);
            await WaitForBlazor();
            await WaitForOptionContaining(companySelects.Nth(1), Company2);
            await SelectFirstOptionContaining(companySelects.Nth(1), Company2);
            await WaitForBlazor();
        }

        var amountInput = Page.Locator("input[type='number']").First;
        if (await amountInput.IsVisibleAsync())
            await amountInput.FillAsync("15000.00");

        var descInput = Page.Locator("input[type='text'], textarea").First;
        if (await descInput.IsVisibleAsync())
            await descInput.FillAsync("Management fee - Q1 2026");

        await TakeScreenshot("s4_inter_company_filled");

        var saveBtn = Page.Locator("button:text('Save'), button:text('Create'), button:text('Submit')").First;
        if (await saveBtn.IsVisibleAsync())
        {
            await saveBtn.ClickAsync();
            await WaitForBlazor();
        }

        await TakeScreenshot("s4_inter_company_saved");
    }

    // --- Step 10: View Consolidated Balance Sheet ---

    [Fact]
    public async Task Step10_ConsolidatedBalanceSheet()
    {
        await EnsureCompany(Company1);
        await EnsureCompany(Company2);

        await NavigateTo("/reports/consolidated-balance-sheet");
        await WaitForBlazor();

        await TakeScreenshot("s4_consolidated_balance_sheet");
    }

    // --- Step 11: View Consolidated Income Statement ---

    [Fact]
    public async Task Step11_ConsolidatedIncomeStatement()
    {
        await EnsureCompany(Company1);
        await EnsureCompany(Company2);

        await NavigateTo("/reports/consolidated-income-statement");
        await WaitForBlazor();

        await TakeScreenshot("s4_consolidated_income_statement");
    }

    // --- Step 12: Company 1 Financial Review ---

    [Fact]
    public async Task Step12_Company1FinancialReview()
    {
        await EnsureCompany(Company1);
        await SelectCompany(Company1);

        await NavigateTo("/reports/balance-sheet");
        await WaitForBlazor();
        await AssertHeading("Balance Sheet");
        await TakeScreenshot("s4_c1_balance_sheet");

        await NavigateTo("/reports/income-statement");
        await WaitForBlazor();
        await AssertHeading("Income Statement");
        await TakeScreenshot("s4_c1_income_statement");
    }

    // --- Step 13: Company 2 Financial Review ---

    [Fact]
    public async Task Step13_Company2FinancialReview()
    {
        await EnsureCompany(Company2);
        await SelectCompany(Company2);

        await NavigateTo("/reports/balance-sheet");
        await WaitForBlazor();
        await AssertHeading("Balance Sheet");
        await TakeScreenshot("s4_c2_balance_sheet");

        await NavigateTo("/reports/income-statement");
        await WaitForBlazor();
        await AssertHeading("Income Statement");
        await TakeScreenshot("s4_c2_income_statement");
    }
}
