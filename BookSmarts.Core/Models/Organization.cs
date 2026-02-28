using BookSmarts.Core.Encryption;

namespace BookSmarts.Core.Models;

public class Organization : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(Organization);
    public string AccountId { get; set; } = "";
    public string Name { get; set; } = "";
    public bool DivisionsEnabled { get; set; }
    public string DefaultCurrency { get; set; } = "USD";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}
