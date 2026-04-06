using Microsoft.Playwright;

namespace BookSmarts.E2E;

/// <summary>
/// Base class for BookSmarts E2E tests. Manages browser lifecycle and provides
/// common helpers for navigating the app.
/// </summary>
public abstract class BookSmartsTestBase : IAsyncLifetime
{
    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser Browser { get; private set; } = null!;
    protected IBrowserContext Context { get; set; } = null!;
    protected IPage Page { get; set; } = null!;

    protected virtual string BaseUrl => TestConfig.BaseUrl;
    protected virtual bool Headless => TestConfig.Headless;

    public virtual async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            SlowMo = TestConfig.SlowMo
        });
        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });
        Page = await Context.NewPageAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (Context != null) await Context.DisposeAsync();
        if (Browser != null) await Browser.DisposeAsync();
        Playwright?.Dispose();
    }

    /// <summary>Navigate to a BookSmarts route (e.g. "/journal-entries").</summary>
    protected async Task NavigateTo(string path)
    {
        var url = $"{BaseUrl}{path}";
        await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForBlazor();
        await OnAfterNavigation();
    }

    /// <summary>
    /// Hook called after every navigation. Override in subclasses to handle
    /// post-navigation concerns like encryption PIN entry.
    /// </summary>
    protected virtual Task OnAfterNavigation() => Task.CompletedTask;

    /// <summary>Wait for Blazor to finish rendering by checking for a stable DOM.</summary>
    protected async Task WaitForBlazor(int timeoutMs = 10000)
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle,
            new PageWaitForLoadStateOptions { Timeout = timeoutMs });
    }

    /// <summary>Take a screenshot and save it to the test output folder.</summary>
    protected async Task<string> TakeScreenshot(string name)
    {
        var dir = Path.Combine(TestConfig.ScreenshotDir, GetType().Name);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
        await Page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
        return path;
    }

    /// <summary>Click a sidebar nav item by its displayed text.</summary>
    protected async Task ClickNavItem(string text)
    {
        await Page.Locator($".bs-nav-item:has(span:text('{text}'))").ClickAsync();
        await WaitForBlazor();
    }

    /// <summary>Assert the page has a heading with the given text.</summary>
    protected async Task AssertHeading(string text)
    {
        var heading = Page.Locator($"h1:text('{text}'), h2:text('{text}'), h3:text('{text}')").First;
        await Assertions.Expect(heading).ToBeVisibleAsync();
    }
}
