namespace BookSmarts.E2E.Tests.Scenarios;

/// <summary>
/// Scenario 3: Professional Services Firm (Law Office)
///
/// Models a small law firm with billable hours, retainer payments,
/// budgets, and cash flow forecasting. Exercises budget management,
/// cash forecast, and the business coach AI feature.
/// </summary>
public class ProfessionalServicesTests : ScenarioTestBase
{
    private static readonly string CompanyName = $"Morrison & Associates Law ({RunId})";

    // --- Step 1: Setup Company ---

    [Fact]
    public async Task Step1_SetupCompany()
    {
        await EnsureCompany(CompanyName);

        await Assertions.Expect(Page.Locator(".bs-sidebar").First).ToBeVisibleAsync(
            new() { Timeout = 10000 });

        await TakeScreenshot("s3_company_ready");
    }

    // --- Step 2: Add Clients ---

    [Fact]
    public async Task Step2_AddClients()
    {
        await EnsureCompany(CompanyName);

        await EnsureCustomer("TechStart Inc", "James Chen", "james@techstart.io");
        await TakeScreenshot("s3_client1_added");

        await EnsureCustomer("Green Valley Properties", "Maria Santos", "maria@gvproperties.com");
        await TakeScreenshot("s3_client2_added");
    }

    // --- Step 3: Add Vendors ---

    [Fact]
    public async Task Step3_AddVendors()
    {
        await EnsureCompany(CompanyName);

        await EnsureVendor("Westlaw", "billing@westlaw.com");
        await TakeScreenshot("s3_vendor1_added");

        await EnsureVendor("Metro Office Suites", "leasing@metrooffice.com");
        await TakeScreenshot("s3_vendor2_added");
    }

    // --- Step 4: Invoice Clients ---

    [Fact]
    public async Task Step4_InvoiceClients()
    {
        await EnsureCompany(CompanyName);
        await EnsureCustomer("TechStart Inc", "James Chen", "james@techstart.io");
        await EnsureCustomer("Green Valley Properties", "Maria Santos", "maria@gvproperties.com");

        await CreateInvoiceForCustomer("TechStart Inc", "Legal consultation - IP review (8 hrs)", 8, 350m);
        await TakeScreenshot("s3_invoice1_saved");

        await CreateInvoiceForCustomer("Green Valley Properties", "Real estate closing services", 1, 2500m);
        await TakeScreenshot("s3_invoice2_saved");
    }

    // --- Step 5: Create Vendor Bills ---

    [Fact]
    public async Task Step5_CreateBills()
    {
        await EnsureCompany(CompanyName);
        await EnsureVendor("Westlaw", "billing@westlaw.com");
        await EnsureVendor("Metro Office Suites", "leasing@metrooffice.com");

        await CreateBillForVendor("Westlaw", "Legal research subscription - March", 1, 450m);
        await TakeScreenshot("s3_bill1_saved");

        await CreateBillForVendor("Metro Office Suites", "Office lease - March 2026", 1, 3200m);
        await TakeScreenshot("s3_bill2_saved");
    }

    // --- Step 6: Receive Client Payment ---

    [Fact]
    public async Task Step6_ReceivePayment()
    {
        await EnsureCompany(CompanyName);
        await EnsureCustomer("TechStart Inc", "James Chen", "james@techstart.io");
        await CreateInvoiceDraft("Legal consultation", 2800m);

        await ReceivePayment("TechStart Inc", 2800m);
        await TakeScreenshot("s3_payment_received");
    }

    // --- Step 7: Pay Vendor ---

    [Fact]
    public async Task Step7_PayVendor()
    {
        await EnsureCompany(CompanyName);
        await EnsureVendor("Metro Office Suites", "leasing@metrooffice.com");
        await CreateBillDraft("Office lease", 3200m);

        await MakePayment("Metro Office Suites", 3200m);
        await TakeScreenshot("s3_vendor_payment_made");
    }

    // --- Step 8: View Budgets Page ---

    [Fact]
    public async Task Step8_ViewBudgets()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/budgets");
        await WaitForBlazor();
        await AssertHeading("Budgets");
        await TakeScreenshot("s3_budgets_page");
    }

    // --- Step 9: View Cash Forecast (AI) ---

    [Fact]
    public async Task Step9_ViewCashForecast()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/ai/cash-forecast");
        await WaitForBlazor();

        await AssertHeading("Cash Flow Forecast");
        await TakeScreenshot("s3_cash_forecast");
    }

    // --- Step 10: View Business Coach / AI Chat ---

    [Fact]
    public async Task Step10_ViewBusinessCoach()
    {
        await EnsureCompany(CompanyName);
        await SelectCompany(CompanyName);

        await NavigateTo("/ai/chat");
        await WaitForBlazor();

        await AssertHeading("AI Business Coach");
        await TakeScreenshot("s3_business_coach");
    }

    // --- Step 11: View Budget vs Actual ---

    [Fact]
    public async Task Step11_ViewBudgetVsActual()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/reports/budget-vs-actual");
        await WaitForBlazor();
        await AssertHeading("Budget vs Actual");
        await TakeScreenshot("s3_budget_vs_actual");
    }

    // --- Step 12: Full Financial Review ---

    [Fact]
    public async Task Step12_FinancialReview()
    {
        await EnsureCompany(CompanyName);

        await NavigateTo("/reports/balance-sheet");
        await WaitForBlazor();
        await AssertHeading("Balance Sheet");
        await TakeScreenshot("s3_balance_sheet");

        await NavigateTo("/reports/income-statement");
        await WaitForBlazor();
        await AssertHeading("Income Statement");
        await TakeScreenshot("s3_income_statement");

        await NavigateTo("/trial-balance");
        await WaitForBlazor();
        await AssertHeading("Trial Balance");
        await TakeScreenshot("s3_trial_balance");

        await NavigateTo("/reports/ar-aging");
        await WaitForBlazor();
        await AssertHeading("AR Aging");
        await TakeScreenshot("s3_ar_aging");

        await NavigateTo("/reports/ap-aging");
        await WaitForBlazor();
        await AssertHeading("AP Aging");
        await TakeScreenshot("s3_ap_aging");
    }
}
