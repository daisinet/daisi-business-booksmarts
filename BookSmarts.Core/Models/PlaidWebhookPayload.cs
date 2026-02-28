namespace BookSmarts.Core.Models;

public class PlaidWebhookPayload
{
    public string? WebhookType { get; set; }
    public string? WebhookCode { get; set; }
    public string? ItemId { get; set; }
    public PlaidWebhookError? Error { get; set; }
}

public class PlaidWebhookError
{
    public string? ErrorType { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
