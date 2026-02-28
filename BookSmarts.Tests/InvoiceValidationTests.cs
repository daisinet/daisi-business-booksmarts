using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class InvoiceValidationTests
{
    [Fact]
    public void ValidateInvoice_ValidInvoice_Succeeds()
    {
        var invoice = MakeValidInvoice();
        var ex = Record.Exception(() => InvoiceService.ValidateInvoice(invoice));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateInvoice_MissingCompanyId_Throws()
    {
        var invoice = MakeValidInvoice();
        invoice.CompanyId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => InvoiceService.ValidateInvoice(invoice));
        Assert.Contains("Company ID", ex.Message);
    }

    [Fact]
    public void ValidateInvoice_MissingCustomerId_Throws()
    {
        var invoice = MakeValidInvoice();
        invoice.CustomerId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => InvoiceService.ValidateInvoice(invoice));
        Assert.Contains("Customer", ex.Message);
    }

    [Fact]
    public void ValidateInvoice_NoLines_Throws()
    {
        var invoice = MakeValidInvoice();
        invoice.Lines.Clear();
        var ex = Assert.Throws<InvalidOperationException>(() => InvoiceService.ValidateInvoice(invoice));
        Assert.Contains("at least one line", ex.Message);
    }

    [Fact]
    public void ValidateInvoice_ZeroQuantity_Throws()
    {
        var invoice = MakeValidInvoice();
        invoice.Lines[0].Quantity = 0;
        var ex = Assert.Throws<InvalidOperationException>(() => InvoiceService.ValidateInvoice(invoice));
        Assert.Contains("quantity", ex.Message);
    }

    [Fact]
    public void ValidateInvoice_NegativeUnitPrice_Throws()
    {
        var invoice = MakeValidInvoice();
        invoice.Lines[0].UnitPrice = -10;
        var ex = Assert.Throws<InvalidOperationException>(() => InvoiceService.ValidateInvoice(invoice));
        Assert.Contains("negative", ex.Message);
    }

    [Fact]
    public void ValidateInvoice_MissingAccountId_Throws()
    {
        var invoice = MakeValidInvoice();
        invoice.Lines[0].AccountId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => InvoiceService.ValidateInvoice(invoice));
        Assert.Contains("revenue account", ex.Message);
    }

    [Fact]
    public void ValidateInvoice_MissingDescription_Throws()
    {
        var invoice = MakeValidInvoice();
        invoice.Lines[0].Description = "";
        var ex = Assert.Throws<InvalidOperationException>(() => InvoiceService.ValidateInvoice(invoice));
        Assert.Contains("description", ex.Message);
    }

    [Theory]
    [InlineData(PaymentTerms.DueOnReceipt, 0)]
    [InlineData(PaymentTerms.Net15, 15)]
    [InlineData(PaymentTerms.Net30, 30)]
    [InlineData(PaymentTerms.Net60, 60)]
    [InlineData(PaymentTerms.Net90, 90)]
    public void ComputeDueDate_ReturnsCorrectDate(PaymentTerms terms, int expectedDays)
    {
        var invoiceDate = new DateTime(2026, 1, 1);
        var dueDate = InvoiceService.ComputeDueDate(invoiceDate, terms);
        Assert.Equal(invoiceDate.AddDays(expectedDays), dueDate);
    }

    [Fact]
    public void Invoice_Subtotal_CalculatedFromLines()
    {
        var invoice = MakeValidInvoice();
        // Line 1: 2 x 100 = 200, Line 2: 1 x 50 = 50
        Assert.Equal(250m, invoice.Subtotal);
        Assert.Equal(250m, invoice.Total);
    }

    [Fact]
    public void Invoice_BalanceDue_ReflectsAmountPaid()
    {
        var invoice = MakeValidInvoice();
        invoice.AmountPaid = 100;
        Assert.Equal(150m, invoice.BalanceDue);
    }

    private static Invoice MakeValidInvoice() => new()
    {
        CompanyId = "co-test",
        CustomerId = "cust-1",
        CustomerName = "Test Customer",
        InvoiceDate = new DateTime(2026, 1, 15),
        PaymentTerms = PaymentTerms.Net30,
        Lines = new()
        {
            new InvoiceLine { Description = "Service A", Quantity = 2, UnitPrice = 100, AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue" },
            new InvoiceLine { Description = "Service B", Quantity = 1, UnitPrice = 50, AccountId = "rev-1", AccountNumber = "4000", AccountName = "Revenue" },
        }
    };
}
