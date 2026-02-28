namespace BookSmarts.SDK.Models;

public class Invoice
{
    public string Id { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string? InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string CustomerId { get; set; } = "";
    public string? CustomerName { get; set; }
    public string Status { get; set; } = "Draft";
    public string PaymentTerms { get; set; } = "Net30";
    public List<InvoiceLine> Lines { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string? Notes { get; set; }
    public string? Memo { get; set; }
    public string? JournalEntryId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class InvoiceLine
{
    public int LineNumber { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? AccountId { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
}
