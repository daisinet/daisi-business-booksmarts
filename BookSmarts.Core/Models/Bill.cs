using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

public class Bill : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(Bill);
    public string CompanyId { get; set; } = "";
    public string BillNumber { get; set; } = "";
    public DateTime BillDate { get; set; }
    public DateTime DueDate { get; set; }
    public string VendorId { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string? VendorReferenceNumber { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Draft;
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public List<BillLine> Lines { get; set; } = new();
    public decimal Subtotal => Lines.Sum(l => l.Amount);
    public decimal Total => Subtotal;
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue => Total - AmountPaid;
    public string? Notes { get; set; }
    public string? Memo { get; set; }
    public string? JournalEntryId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}

public class BillLine
{
    public int LineNumber { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;
    public string AccountId { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
}
