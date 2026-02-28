using BookSmarts.Core.Encryption;

namespace BookSmarts.Core.Models;

public class Company : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(Company);
    public string AccountId { get; set; } = "";
    public string OrganizationId { get; set; } = "";
    public string? DivisionId { get; set; }
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "USD";
    public int FiscalYearStartMonth { get; set; } = 1;
    public string? TaxId { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}
