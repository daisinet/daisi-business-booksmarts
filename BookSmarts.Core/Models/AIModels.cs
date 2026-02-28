namespace BookSmarts.Core.Models;

/// <summary>
/// In-memory chat conversation for the AI Business Coach. Not persisted.
/// </summary>
public class ChatConversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CompanyId { get; set; } = "";
    public string Title { get; set; } = "New Conversation";
    public List<ChatEntry> Messages { get; set; } = new();
}

/// <summary>
/// A single message in a chat conversation.
/// </summary>
public class ChatEntry
{
    public string Role { get; set; } = ""; // "user" or "assistant"
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// AI-generated financial projection result.
/// </summary>
public class ProjectionResult
{
    public List<ProjectedMonth> Months { get; set; } = new();
    public string Assumptions { get; set; } = "";
    public string Risks { get; set; } = "";
    public string Summary { get; set; } = "";
}

/// <summary>
/// A single month within a financial projection.
/// </summary>
public class ProjectedMonth
{
    public string Month { get; set; } = "";
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetIncome { get; set; }
    public decimal CashBalance { get; set; }
}

/// <summary>
/// AI-generated cash flow forecast result.
/// </summary>
public class CashForecast
{
    public decimal CurrentCash { get; set; }
    public List<CashForecastPeriod> Periods { get; set; } = new();
    public string Assumptions { get; set; } = "";
    public string Risks { get; set; } = "";
    public string Summary { get; set; } = "";
}

/// <summary>
/// A single period within a cash forecast (30, 60, or 90 days).
/// </summary>
public class CashForecastPeriod
{
    public string Label { get; set; } = "";
    public decimal ExpectedInflows { get; set; }
    public decimal ExpectedOutflows { get; set; }
    public decimal ProjectedBalance { get; set; }
}
