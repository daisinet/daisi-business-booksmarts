namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for the DAISI account registration flow on the Manager.
/// These test the browser-based registration form at /account/register,
/// which is the entry point for new BookSmarts customers.
/// </summary>
public class RegistrationTests : BookSmartsTestBase
{
    private string ManagerUrl => TestConfig.ManagerUrl;

    [Fact]
    public async Task Register_PageLoads_WithForm()
    {
        await Page.GotoAsync($"{ManagerUrl}/account/register", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        // Form fields should be visible
        await Assertions.Expect(Page.Locator("h3:text('Join the AI Rebellion')")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator("input[placeholder='Your Account Name']")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator("input[placeholder='Kyle Reese']")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator("input[placeholder*='skynet']")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator("input[placeholder='555-555-4242']")).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator("button:text('Join Now')")).ToBeVisibleAsync();

        await TakeScreenshot("register_form_loaded");
    }

    [Fact]
    public async Task Register_Validation_RequiresAllFields()
    {
        await Page.GotoAsync($"{ManagerUrl}/account/register", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        // Click Join Now without filling anything
        await Page.Locator("button:text('Join Now')").ClickAsync();

        // Should show validation error
        var error = Page.Locator(".alert-danger");
        await Assertions.Expect(error).ToBeVisibleAsync();

        await TakeScreenshot("register_validation_error");
    }

    [Fact]
    public async Task Register_SuccessfulRegistration()
    {
        await Page.GotoAsync($"{ManagerUrl}/account/register", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Fill in all required fields
        await Page.Locator("input[placeholder='Your Account Name']")
            .FillAsync($"E2E Registration Test {uniqueId}");

        await Page.Locator("input[placeholder='Kyle Reese']")
            .FillAsync("E2E Test User");

        await Page.Locator("input[placeholder*='skynet']")
            .FillAsync($"e2e-{uniqueId}@test.booksmarts.local");

        await Page.Locator("input[placeholder='555-555-4242']")
            .FillAsync("555-000-1234");

        // Check both consent checkboxes
        await Page.Locator("#switchTerms").CheckAsync();
        await Page.Locator("#switchContact").CheckAsync();

        await TakeScreenshot("register_form_filled");

        // Submit
        await Page.Locator("button:text('Join Now')").ClickAsync();

        // Wait for success message
        var success = Page.Locator(".alert-success");
        await Assertions.Expect(success).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Assertions.Expect(success).ToContainTextAsync("account has been created successfully");

        // "Continue to Login" link should appear
        await Assertions.Expect(Page.Locator("a:text('Continue to Login')")).ToBeVisibleAsync();

        await TakeScreenshot("register_success");
    }

    [Fact]
    public async Task Register_ThenLoginToBookSmarts_ViaSSO()
    {
        // Create a fresh user programmatically (faster than filling form)
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var user = TestAccountHelper.CreateUser(
            "E2E SSO User",
            $"e2e-sso-{uniqueId}@test.booksmarts.local",
            "555-000-5678"
        );

        // If the account has encryption, the new user needs the PIN too
        user = user with { EncryptionPin = TestConfig.TestUserEncryptionPin };

        // Generate SSO ticket and navigate to BookSmarts
        var ticket = TestAuthHelper.CreateSsoTicketForUser(user);
        var callbackUrl = $"{BaseUrl}/sso/callback?ticket={Uri.EscapeDataString(ticket)}";
        await Page.GotoAsync(callbackUrl, new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });
        await WaitForBlazor();

        // Handle encryption PIN if the account has it enabled
        await HandlePinIfPresent(user);

        // Should land on the dashboard with the sidebar visible
        var sidebar = Page.Locator(".bs-sidebar");
        await Assertions.Expect(sidebar).ToBeVisibleAsync(new() { Timeout = 10000 });

        await TakeScreenshot("register_then_sso_dashboard");
    }

    private async Task HandlePinIfPresent(TestUserProfile user)
    {
        var pinOverlay = Page.Locator(".bs-pin-overlay");
        if (!await pinOverlay.IsVisibleAsync())
            return;

        var pin = user.EncryptionPin;
        if (string.IsNullOrEmpty(pin))
            return;

        for (int i = 0; i < pin.Length; i++)
        {
            var input = Page.Locator($"#pin-digit-{i}");
            await input.ClickAsync();
            await Page.Keyboard.PressAsync(pin[i].ToString());
        }

        await pinOverlay.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 20000 });
        await WaitForBlazor();
    }
}
