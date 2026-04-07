namespace BookSmarts.E2E.Tests.Scenarios;

/// <summary>
/// Shared base class for business scenario tests.
/// Provides idempotent setup helpers and UI interaction utilities
/// that all scenario test classes can reuse.
/// </summary>
public abstract class ScenarioTestBase : AuthenticatedTestBase
{
    /// <summary>
    /// Unique identifier for this test run, shared across all scenario tests.
    /// Appended to company/org/user names so each run creates distinct data.
    /// Format: MMdd-HHmm (e.g. "0331-1523").
    /// </summary>
    protected static readonly string RunId = DateTime.UtcNow.ToString("MMdd-HHmm");

    /// <summary>
    /// Override the test user profile to tag the display name with the run ID.
    /// The UserId/AccountId remain the same (real system IDs), but the display
    /// names include the RunId so data created by each run is identifiable.
    /// </summary>
    protected override TestUserProfile TestUser => TestConfig.DefaultTestUser with
    {
        UserName = $"{TestConfig.TestUserName} [{RunId}]",
        AccountName = $"{TestConfig.TestAccountName} [{RunId}]"
    };

    // =======================================================================
    // Idempotent setup helpers — safe to call multiple times
    // =======================================================================

    protected async Task EnsureOrganization(string? orgName = null)
    {
        orgName ??= $"E2E Test Organization ({RunId})";

        await NavigateTo("/settings/organization");
        await WaitForBlazor();

        if (await Page.Locator("button:text('Update Organization')").IsVisibleAsync())
            return;

        var nameInput = Page.Locator("input[placeholder*='Acme Corp']");
        if (await nameInput.IsVisibleAsync())
        {
            await nameInput.FillAsync(orgName);
            await Page.Locator("select").First.SelectOptionAsync("USD");
            await Page.Locator("button:text('Create Organization')").ClickAsync();
            await WaitForBlazor();
            await Assertions.Expect(Page.Locator("button:text('Update Organization')"))
                .ToBeVisibleAsync(new() { Timeout = 10000 });
        }
    }

    protected async Task EnsureCompany(string companyName)
    {
        await EnsureOrganization();

        var companyOption = Page.Locator($".bs-header select.form-select option:text('{companyName}'), header select option:text('{companyName}')").First;
        if (await companyOption.CountAsync() > 0)
            return;

        await NavigateTo("/settings/companies");
        await WaitForBlazor();

        await Page.Locator("button:text('Add Company'), button:text('Add a Company')").First.ClickAsync();
        await WaitForBlazor();
        var nameInput = Page.Locator("input[placeholder*='West Coast']");
        await nameInput.FillAsync(companyName);
        await nameInput.PressAsync("Tab");

        var seedCoa = Page.Locator("#seedCoaCheck");
        if (!await seedCoa.IsCheckedAsync())
            await seedCoa.CheckAsync();

        await Page.Locator("button:text('Save')").ClickAsync();
        await WaitForBlazor();

        // Navigate to dashboard to ensure the header company selector refreshes
        await NavigateTo("/");
        await WaitForBlazor();
    }

    protected async Task SelectCompany(string companyName)
    {
        var selector = Page.Locator(".bs-header select.form-select, header select").First;
        await SelectFirstOptionContaining(selector, companyName);
        await WaitForBlazor();
    }

    protected async Task EnsureCustomer(string name, string contact, string email)
    {
        await NavigateTo("/customers");
        await WaitForBlazor();

        if (await Page.GetByText(name).IsVisibleAsync())
            return;

        await Page.Locator("button:text('Add Customer'), button:text('Add a Customer')").First.ClickAsync();
        await WaitForBlazor();
        await Page.Locator("input[placeholder*='Company or individual']").FillAsync(name);
        await FillFormField("Contact Person", contact);
        await FillFormField("Email", email);
        await Page.Locator("button:text('Save')").ClickAsync();
        await WaitForBlazor();
    }

    protected async Task EnsureVendor(string name, string email)
    {
        await NavigateTo("/vendors");
        await WaitForBlazor();

        if (await Page.GetByText(name).IsVisibleAsync())
            return;

        await Page.Locator("button:text('Add Vendor'), button:text('Add a Vendor')").First.ClickAsync();
        await WaitForBlazor();
        await Page.Locator("input[placeholder*='Company or individual']").FillAsync(name);
        await FillFormField("Email", email);
        await Page.Locator("button:text('Save')").ClickAsync();
        await WaitForBlazor();
    }

    protected async Task CreateInvoiceDraft(string description, decimal amount)
    {
        await NavigateTo("/invoices/new");
        await WaitForBlazor();

        // Select the first available customer (header row select, not tbody)
        var customerSelect = Page.Locator("div.row select.form-select").First;
        await WaitForNonEmptyOptions(customerSelect);
        await SelectFirstAvailableOption(customerSelect);
        await WaitForBlazor();

        var descInputs = Page.Locator("tbody input[type='text']");
        await descInputs.First.FillAsync(description);

        var numberInputs = Page.Locator("tbody input[type='number']");
        await numberInputs.Nth(0).FillAsync("1");
        await numberInputs.Nth(1).FillAsync(amount.ToString("F2"));

        await SelectFirstAvailableAccount("tbody select");

        await Page.Locator("button:text('Save as Draft')").ClickAsync();
        await WaitForBlazor();
    }

    protected async Task CreateBillDraft(string description, decimal amount)
    {
        await NavigateTo("/bills/new");
        await WaitForBlazor();

        // Select the first available vendor (header row select, not tbody)
        var vendorSelect = Page.Locator("div.row select.form-select").First;
        await WaitForNonEmptyOptions(vendorSelect);
        await SelectFirstAvailableOption(vendorSelect);
        await WaitForBlazor();

        var descInputs = Page.Locator("tbody input[type='text']");
        await descInputs.First.FillAsync(description);

        var numberInputs = Page.Locator("tbody input[type='number']");
        await numberInputs.Nth(0).FillAsync("1");
        await numberInputs.Nth(1).FillAsync(amount.ToString("F2"));

        await SelectFirstAvailableAccount("tbody select");

        await Page.Locator("button:text('Save as Draft')").ClickAsync();
        await WaitForBlazor();
    }

    protected async Task CreateInvoiceForCustomer(string customerName, string description, int qty, decimal unitPrice)
    {
        await NavigateTo("/invoices/new");
        await WaitForBlazor();

        // Target the customer select specifically (first select in the header row, not in tbody)
        var customerSelect = Page.Locator("div.row select.form-select").First;
        await WaitForOptionContaining(customerSelect, customerName);
        await SelectFirstOptionContaining(customerSelect, customerName);
        await WaitForBlazor();

        // Fill line item
        var descInputs = Page.Locator("tbody input[type='text']");
        await descInputs.First.FillAsync(description);

        var numberInputs = Page.Locator("tbody input[type='number']");
        await numberInputs.Nth(0).FillAsync(qty.ToString());
        await numberInputs.Nth(1).FillAsync(unitPrice.ToString("F2"));

        await SelectFirstAvailableAccount("tbody select");

        await Page.Locator("button:text('Save as Draft')").ClickAsync();
        await WaitForBlazor();

        // Verify save succeeded — page should navigate to invoice view (no error message)
        await Assertions.Expect(Page.Locator("text='Customer is required'")).Not.ToBeVisibleAsync(
            new() { Timeout = 5000 });
    }

    protected async Task CreateBillForVendor(string vendorName, string description, int qty, decimal unitPrice)
    {
        await NavigateTo("/bills/new");
        await WaitForBlazor();

        // Target the vendor select specifically (first select in the header row, not in tbody)
        var vendorSelect = Page.Locator("div.row select.form-select").First;
        await WaitForOptionContaining(vendorSelect, vendorName);
        await SelectFirstOptionContaining(vendorSelect, vendorName);
        await WaitForBlazor();

        // Fill line item
        var descInputs = Page.Locator("tbody input[type='text']");
        await descInputs.First.FillAsync(description);

        var numberInputs = Page.Locator("tbody input[type='number']");
        await numberInputs.Nth(0).FillAsync(qty.ToString());
        await numberInputs.Nth(1).FillAsync(unitPrice.ToString("F2"));

        await SelectFirstAvailableAccount("tbody select");

        await Page.Locator("button:text('Save as Draft')").ClickAsync();
        await WaitForBlazor();

        // Verify save succeeded
        await Assertions.Expect(Page.Locator("text='Vendor is required'")).Not.ToBeVisibleAsync(
            new() { Timeout = 5000 });
    }

    protected async Task ReceivePayment(string customerName, decimal amount)
    {
        await NavigateTo("/payments/receive");
        await WaitForBlazor();

        var customerSelect = Page.Locator("div.row select.form-select").First;
        await WaitForOptionContaining(customerSelect, customerName);
        await SelectFirstOptionContaining(customerSelect, customerName);
        await WaitForBlazor();

        var amountInput = Page.Locator("input[type='number']").First;
        await amountInput.FillAsync(amount.ToString("F2"));

        var dateInput = Page.Locator("input[type='date']").First;
        await dateInput.FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));

        await Page.Locator("button:text('Save Payment')").ClickAsync();
        await WaitForBlazor();
    }

    protected async Task MakePayment(string vendorName, decimal amount)
    {
        await NavigateTo("/payments/make");
        await WaitForBlazor();

        var vendorSelect = Page.Locator("div.row select.form-select").First;
        await WaitForOptionContaining(vendorSelect, vendorName);
        await SelectFirstOptionContaining(vendorSelect, vendorName);
        await WaitForBlazor();

        var amountInput = Page.Locator("input[type='number']").First;
        await amountInput.FillAsync(amount.ToString("F2"));

        var dateInput = Page.Locator("input[type='date']").First;
        await dateInput.FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));

        await Page.Locator("button:text('Save Payment')").ClickAsync();
        await WaitForBlazor();
    }

    // =======================================================================
    // UI interaction helpers
    // =======================================================================

    protected async Task FillFormField(string label, string value)
    {
        var formGroup = Page.Locator($"label:text('{label}')").Locator("..");
        var input = formGroup.Locator("input, textarea").First;
        if (await input.IsVisibleAsync())
            await input.FillAsync(value);
    }

    protected async Task SelectFirstAvailableOption(string selector)
    {
        var select = Page.Locator(selector).First;
        await SelectFirstAvailableOption(select);
    }

    protected async Task SelectFirstAvailableOption(ILocator select)
    {
        var options = await select.Locator("option").AllAsync();
        if (options.Count > 1)
            await select.SelectOptionAsync(new SelectOptionValue { Index = 1 });
    }

    /// <summary>
    /// Waits for a select element to have an option containing the given text (async data loading).
    /// </summary>
    protected async Task WaitForOptionContaining(ILocator select, string text, int timeoutMs = 10000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var options = await select.Locator("option").AllAsync();
            foreach (var option in options)
            {
                var optText = await option.TextContentAsync();
                if (optText?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                    return;
            }
            await Task.Delay(250);
        }
    }

    /// <summary>
    /// Waits for a select element to have more than just the placeholder option.
    /// </summary>
    protected async Task WaitForNonEmptyOptions(ILocator select, int timeoutMs = 10000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var options = await select.Locator("option").AllAsync();
            if (options.Count > 1)
                return;
            await Task.Delay(250);
        }
    }

    protected async Task SelectFirstOptionContaining(string selector, string text)
    {
        var select = Page.Locator(selector).First;
        await SelectFirstOptionContaining(select, text);
    }

    protected async Task SelectFirstOptionContaining(ILocator select, string text)
    {
        var options = await select.Locator("option").AllAsync();
        foreach (var option in options)
        {
            var optText = await option.TextContentAsync();
            if (optText?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
            {
                var value = await option.GetAttributeAsync("value");
                if (value != null)
                    await select.SelectOptionAsync(value);
                return;
            }
        }
        if (options.Count > 1)
            await select.SelectOptionAsync(new SelectOptionValue { Index = 1 });
    }

    protected async Task SelectFirstAvailableAccount(string selector)
    {
        var selects = Page.Locator(selector);
        if (await selects.CountAsync() > 0)
        {
            var options = await selects.First.Locator("option").AllAsync();
            if (options.Count > 1)
                await selects.First.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }
    }

    protected async Task AssertCompanyInHeader(string companyName)
    {
        var companySelector = Page.Locator(".bs-header select.form-select, header select");
        await Assertions.Expect(companySelector.Locator($"option:text('{companyName}')")).ToBeAttachedAsync(
            new() { Timeout = 10000 });
    }
}
