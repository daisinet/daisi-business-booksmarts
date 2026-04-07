using Microsoft.Playwright;

namespace BookSmarts.E2E;

/// <summary>
/// Base class for tests that require an authenticated session.
/// Uses the real DAISI SSO flow: generates a user-scoped clientKey via ORC gRPC,
/// builds an encrypted SSO ticket, and passes it to BookSmarts' /sso/callback.
/// No backdoors — this is the same auth path production uses.
/// </summary>
public abstract class AuthenticatedTestBase : BookSmartsTestBase
{
    /// <summary>
    /// Override to use a different user profile for specific test classes
    /// (e.g. a non-owner role, or a different account for multi-company tests).
    /// </summary>
    protected virtual TestUserProfile TestUser => TestConfig.DefaultTestUser;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await LoginViaSsoTicket();
    }

    /// <summary>
    /// After every full page navigation, check for and handle the encryption PIN overlay.
    /// Blazor Server creates a new circuit on each full navigation, resetting EncryptionContext.
    /// </summary>
    protected override Task OnAfterNavigation() => EnterEncryptionPinIfPresent();

    private async Task LoginViaSsoTicket()
    {
        // Generate an encrypted SSO ticket for the test user via ORC gRPC
        var ticket = TestAuthHelper.CreateSsoTicketForUser(TestUser);

        // Navigate to the SSO callback endpoint — this is the exact same URL
        // that the Manager redirects to during real SSO login
        var callbackUrl = $"{BaseUrl}/sso/callback?ticket={Uri.EscapeDataString(ticket)}";
        await Page.GotoAsync(callbackUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        // The middleware decrypts the ticket, creates a local clientKey,
        // sets session cookies, and redirects to /
        await WaitForBlazor();

        // If the account has encryption enabled, handle the PIN entry overlay
        await EnterEncryptionPinIfPresent();
    }

    private async Task EnterEncryptionPinIfPresent()
    {
        var pinOverlay = Page.Locator(".bs-pin-overlay");
        var isVisible = await pinOverlay.IsVisibleAsync();

        if (!isVisible)
            return;

        var pin = TestUser.EncryptionPin;
        if (string.IsNullOrEmpty(pin))
            throw new InvalidOperationException(
                "PIN overlay detected but no EncryptionPin configured. " +
                "Add TestUser:EncryptionPin to testsettings.json.");

        // Click and type each digit — click ensures focus, Press fires key events
        // that Blazor's @oninput handler will pick up.
        for (int i = 0; i < pin.Length; i++)
        {
            var input = Page.Locator($"#pin-digit-{i}");
            await input.ClickAsync();
            await Page.Keyboard.PressAsync(pin[i].ToString());
        }

        // The component auto-submits when all 6 digits are filled.
        // Wait for either: overlay disappears (success) or error message appears (wrong PIN).
        var hiddenTask = pinOverlay.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 20000
        });

        var errorLocator = Page.Locator(".bs-pin-overlay .text-danger");
        var errorTask = errorLocator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 20000
        });

        await Task.WhenAny(hiddenTask, errorTask);

        // Check if we got an error instead of success
        if (await errorLocator.IsVisibleAsync())
        {
            var errorText = await errorLocator.TextContentAsync();
            await TakeScreenshot("auth_pin_failed");
            throw new InvalidOperationException(
                $"PIN verification failed: {errorText}. Check TestUser:EncryptionPin in testsettings.json.");
        }

        await WaitForBlazor();
    }
}
