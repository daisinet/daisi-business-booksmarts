using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

/// <summary>
/// A budget for a company's fiscal year, containing line items per account per period.
/// Persisted in the Budgets container with CompanyId as partition key.
/// </summary>
public class Budget : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(Budget);
    public string CompanyId { get; set; } = "";
    public string FiscalYearId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;
    public List<BudgetLineItem> LineItems { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}

/// <summary>
/// A single budget amount for one account in one fiscal period.
/// </summary>
public class BudgetLineItem
{
    public string AccountId { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string PeriodId { get; set; } = "";
    public int PeriodNumber { get; set; }
    public decimal Amount { get; set; }
}
