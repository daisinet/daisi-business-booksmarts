using BookSmarts.Core.Encryption;

namespace BookSmarts.Core.Models;

public class Division : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(Division);
    public string AccountId { get; set; } = "";
    public string OrganizationId { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? EncryptedPayload { get; set; }
}
