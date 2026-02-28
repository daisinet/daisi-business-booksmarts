namespace BookSmarts.Core.Enums;

public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Voided = 5,
    WrittenOff = 6
}
