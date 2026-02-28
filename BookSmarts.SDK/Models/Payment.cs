namespace BookSmarts.SDK.Models;

public class Payment
{
    public string Id { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string? PaymentNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentType { get; set; } = "CustomerPayment";
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? BankAccountId { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
    public List<PaymentAllocation> Allocations { get; set; } = [];
    public decimal AllocatedAmount { get; set; }
    public decimal UnappliedAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? JournalEntryId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class PaymentAllocation
{
    public string? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? BillId { get; set; }
    public string? BillNumber { get; set; }
    public decimal Amount { get; set; }
}
