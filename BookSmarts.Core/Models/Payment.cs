using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

public class Payment : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(Payment);
    public string CompanyId { get; set; } = "";
    public string PaymentNumber { get; set; } = "";
    public DateTime PaymentDate { get; set; }
    public PaymentType PaymentType { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;
    public string BankAccountId { get; set; } = "";
    public string BankAccountNumber { get; set; } = "";
    public string BankAccountName { get; set; } = "";
    public List<PaymentAllocation> Allocations { get; set; } = new();
    public decimal AllocatedAmount => Allocations.Sum(a => a.Amount);
    public decimal UnappliedAmount => Amount - AllocatedAmount;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? JournalEntryId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string? EncryptedPayload { get; set; }
}

public class PaymentAllocation
{
    public string? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? BillId { get; set; }
    public string? BillNumber { get; set; }
    public decimal Amount { get; set; }
}
