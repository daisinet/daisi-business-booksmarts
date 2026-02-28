using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

public class Customer : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(Customer);
    public string CompanyId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public PaymentTerms DefaultPaymentTerms { get; set; } = PaymentTerms.Net30;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}
