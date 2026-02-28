using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

/// <summary>
/// An inter-company transaction linking journal entries between two companies.
/// Persisted in the InterCompany container with OrganizationId as partition key.
/// </summary>
public class InterCompanyTransaction : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(InterCompanyTransaction);
    public string OrganizationId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string SourceCompanyId { get; set; } = "";
    public string SourceCompanyName { get; set; } = "";
    public string TargetCompanyId { get; set; } = "";
    public string TargetCompanyName { get; set; } = "";
    public string SourceJournalEntryId { get; set; } = "";
    public string TargetJournalEntryId { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public InterCompanyStatus Status { get; set; } = InterCompanyStatus.Posted;
    public bool EliminateOnConsolidation { get; set; } = true;
    public string SourceAccountId { get; set; } = "";
    public string SourceAccountNumber { get; set; } = "";
    public string TargetAccountId { get; set; } = "";
    public string TargetAccountNumber { get; set; } = "";
    public string SourceIcAccountId { get; set; } = "";
    public string TargetIcAccountId { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? EncryptedPayload { get; set; }
}
