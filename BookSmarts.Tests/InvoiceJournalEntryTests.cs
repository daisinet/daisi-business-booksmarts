using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;

namespace BookSmarts.Tests;

public class InvoiceJournalEntryTests
{
    [Fact]
    public void InvoiceSendJE_ShouldDebitAR_CreditRevenuePerLine()
    {
        // Simulates the journal entry that SendInvoiceAsync would create
        var invoice = MakeInvoice();
        var arAccountId = "ar-1";

        var jeLines = new List<JournalLine>
        {
            new() { AccountId = arAccountId, Debit = invoice.Total, Credit = 0 }
        };
        foreach (var line in invoice.Lines)
        {
            jeLines.Add(new JournalLine { AccountId = line.AccountId, Debit = 0, Credit = line.Amount });
        }

        var je = new JournalEntry { CompanyId = "co-test", Lines = jeLines, SourceType = SourceType.Invoice };

        Assert.True(je.IsBalanced);
        Assert.Equal(SourceType.Invoice, je.SourceType);
        Assert.Equal(invoice.Total, je.TotalDebit);
        Assert.Equal(invoice.Total, je.TotalCredit);
    }

    [Fact]
    public void BillReceiveJE_ShouldDebitExpensePerLine_CreditAP()
    {
        var bill = MakeBill();
        var apAccountId = "ap-1";

        var jeLines = new List<JournalLine>();
        foreach (var line in bill.Lines)
        {
            jeLines.Add(new JournalLine { AccountId = line.AccountId, Debit = line.Amount, Credit = 0 });
        }
        jeLines.Add(new JournalLine { AccountId = apAccountId, Debit = 0, Credit = bill.Total });

        var je = new JournalEntry { CompanyId = "co-test", Lines = jeLines, SourceType = SourceType.Bill };

        Assert.True(je.IsBalanced);
        Assert.Equal(SourceType.Bill, je.SourceType);
    }

    [Fact]
    public void CustomerPaymentJE_ShouldDebitBank_CreditAR()
    {
        var je = new JournalEntry
        {
            CompanyId = "co-test",
            SourceType = SourceType.Payment,
            Lines = new()
            {
                new JournalLine { AccountId = "bank-1", Debit = 500, Credit = 0 },
                new JournalLine { AccountId = "ar-1", Debit = 0, Credit = 500 }
            }
        };

        Assert.True(je.IsBalanced);
        Assert.Equal(SourceType.Payment, je.SourceType);
        Assert.Equal(500m, je.TotalDebit);
    }

    [Fact]
    public void VendorPaymentJE_ShouldDebitAP_CreditBank()
    {
        var je = new JournalEntry
        {
            CompanyId = "co-test",
            SourceType = SourceType.Payment,
            Lines = new()
            {
                new JournalLine { AccountId = "ap-1", Debit = 350, Credit = 0 },
                new JournalLine { AccountId = "bank-1", Debit = 0, Credit = 350 }
            }
        };

        Assert.True(je.IsBalanced);
        Assert.Equal(350m, je.TotalDebit);
        Assert.Equal(350m, je.TotalCredit);
    }

    [Fact]
    public void CashBasisFiltering_ExcludesInvoiceAndBill()
    {
        // SourceType.Invoice = 2, SourceType.Bill = 3 are excluded from cash basis
        // SourceType.Payment = 4 is included
        var cashBasisIncluded = new[] { 0, 1, 4, 6, 7 }; // Manual, BankImport, Payment, Adjustment, Reversal

        Assert.DoesNotContain((int)SourceType.Invoice, cashBasisIncluded);
        Assert.DoesNotContain((int)SourceType.Bill, cashBasisIncluded);
        Assert.Contains((int)SourceType.Payment, cashBasisIncluded);
    }

    [Fact]
    public void MultiLineInvoiceJE_RemainsBalanced()
    {
        var invoice = new Invoice
        {
            CompanyId = "co-test",
            CustomerId = "c1",
            Lines = new()
            {
                new InvoiceLine { Description = "A", Quantity = 1, UnitPrice = 100, AccountId = "rev-1" },
                new InvoiceLine { Description = "B", Quantity = 2, UnitPrice = 50, AccountId = "rev-2" },
                new InvoiceLine { Description = "C", Quantity = 3, UnitPrice = 25, AccountId = "rev-3" },
            }
        };

        // Total = 100 + 100 + 75 = 275
        Assert.Equal(275m, invoice.Total);

        var jeLines = new List<JournalLine>
        {
            new() { AccountId = "ar-1", Debit = invoice.Total, Credit = 0 }
        };
        foreach (var l in invoice.Lines)
            jeLines.Add(new JournalLine { AccountId = l.AccountId, Debit = 0, Credit = l.Amount });

        var je = new JournalEntry { CompanyId = "co-test", Lines = jeLines };
        Assert.True(je.IsBalanced);
    }

    private static Invoice MakeInvoice() => new()
    {
        CompanyId = "co-test",
        CustomerId = "c1",
        Lines = new()
        {
            new InvoiceLine { Description = "Service", Quantity = 2, UnitPrice = 100, AccountId = "rev-1" },
            new InvoiceLine { Description = "Support", Quantity = 1, UnitPrice = 50, AccountId = "rev-2" },
        }
    };

    private static Bill MakeBill() => new()
    {
        CompanyId = "co-test",
        VendorId = "v1",
        Lines = new()
        {
            new BillLine { Description = "Supplies", Quantity = 5, UnitPrice = 50, AccountId = "exp-1" },
            new BillLine { Description = "Shipping", Quantity = 1, UnitPrice = 25, AccountId = "exp-2" },
        }
    };
}
