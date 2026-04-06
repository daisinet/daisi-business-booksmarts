namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for AI features: AI Chat (Business Coach), Cash Forecast, and Projections.
/// </summary>
public class AIFeaturesTests : AuthenticatedTestBase
{
    [Fact]
    public async Task AIChat_NavigateAndLoad()
    {
        await NavigateTo("/ai/chat");
        await WaitForBlazor();
        await TakeScreenshot("ai_chat");
    }

    [Fact]
    public async Task AIChat_SendMessage()
    {
        await NavigateTo("/ai/chat");
        await WaitForBlazor();

        // Find the chat input and send a test message
        var chatInput = Page.Locator("textarea, input[type='text']").Last;
        if (await chatInput.IsVisibleAsync())
        {
            await chatInput.FillAsync("What is my current cash balance?");
            await TakeScreenshot("ai_chat_message_typed");

            // Submit (Enter key or send button)
            var sendBtn = Page.Locator("button:has(i[class*='send']), button:text('Send')").First;
            if (await sendBtn.IsVisibleAsync())
            {
                await sendBtn.ClickAsync();
            }
            else
            {
                await chatInput.PressAsync("Enter");
            }

            // Wait for response
            await Page.WaitForTimeoutAsync(5000);
            await TakeScreenshot("ai_chat_response");
        }
    }

    [Fact]
    public async Task CashForecast_NavigateAndLoad()
    {
        await NavigateTo("/ai/cash-forecast");
        await WaitForBlazor();
        await TakeScreenshot("cash_forecast");
    }

    [Fact]
    public async Task Projections_NavigateAndLoad()
    {
        await NavigateTo("/ai/projections");
        await WaitForBlazor();
        await TakeScreenshot("projections");
    }
}
