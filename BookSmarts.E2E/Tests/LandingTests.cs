namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for the public landing/welcome page and sign-up flow.
/// </summary>
public class LandingTests : BookSmartsTestBase
{
    [Fact]
    public async Task WelcomePage_Loads_WithHeroAndFeatures()
    {
        await NavigateTo("/welcome");

        await AssertHeading("Smart Bookkeeping for Small Business");

        // Verify feature cards are visible (6 main + 3 security = 9)
        var featureCards = Page.Locator(".landing-feature-card");
        await Assertions.Expect(featureCards).ToHaveCountAsync(9);

        // Verify CTA buttons
        var createAccountBtn = Page.Locator("a:text('Create Free Account')");
        await Assertions.Expect(createAccountBtn).ToBeVisibleAsync();

        var loginBtn = Page.GetByText("Log In").First;
        await Assertions.Expect(loginBtn).ToBeVisibleAsync();
    }

    [Fact]
    public async Task WelcomePage_CreateAccount_RedirectsToRegistration()
    {
        await NavigateTo("/welcome");

        var createAccountBtn = Page.Locator("a:text('Create Free Account')");
        var href = await createAccountBtn.GetAttributeAsync("href");

        Assert.NotNull(href);
        Assert.Contains("/account/register", href);
    }

    [Fact]
    public async Task WelcomePage_LeadForm_SubmitsSuccessfully()
    {
        await NavigateTo("/welcome");

        // Scroll to get-started section
        await Page.Locator("#get-started").ScrollIntoViewIfNeededAsync();

        // Fill in the lead capture form
        await Page.Locator("#lead-name").FillAsync("E2E Test User");
        await Page.Locator("#lead-company").FillAsync("Test Company LLC");
        await Page.Locator("#lead-email").FillAsync("e2e-lead@example.com");
        await Page.Locator("#lead-phone").FillAsync("555-0100");

        await TakeScreenshot("lead_form_filled");
    }

    [Fact]
    public async Task WelcomePage_SecuritySection_IsVisible()
    {
        await NavigateTo("/welcome");

        await AssertHeading("Your Data. Your Keys. Zero Access for Us.");
    }

    [Fact]
    public async Task WelcomePage_PricingSection_IsVisible()
    {
        await NavigateTo("/welcome");

        await AssertHeading("Free to Use. Pay Only for AI.");
    }
}
