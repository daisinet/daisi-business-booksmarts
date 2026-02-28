namespace BookSmarts.Core.Models;

/// <summary>
/// Request DTO for creating an inter-company transaction.
/// </summary>
public class InterCompanyTransactionRequest
{
    public string OrganizationId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string SourceCompanyId { get; set; } = "";
    public string TargetCompanyId { get; set; } = "";
    public string SourceAccountId { get; set; } = "";
    public string TargetAccountId { get; set; } = "";
    public string SourceIcAccountId { get; set; } = "";
    public string TargetIcAccountId { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
}
