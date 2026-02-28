using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class BillValidationTests
{
    [Fact]
    public void ValidateBill_ValidBill_Succeeds()
    {
        var bill = MakeValidBill();
        var ex = Record.Exception(() => BillService.ValidateBill(bill));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateBill_MissingCompanyId_Throws()
    {
        var bill = MakeValidBill();
        bill.CompanyId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => BillService.ValidateBill(bill));
        Assert.Contains("Company ID", ex.Message);
    }

    [Fact]
    public void ValidateBill_MissingVendorId_Throws()
    {
        var bill = MakeValidBill();
        bill.VendorId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => BillService.ValidateBill(bill));
        Assert.Contains("Vendor", ex.Message);
    }

    [Fact]
    public void ValidateBill_NoLines_Throws()
    {
        var bill = MakeValidBill();
        bill.Lines.Clear();
        var ex = Assert.Throws<InvalidOperationException>(() => BillService.ValidateBill(bill));
        Assert.Contains("at least one line", ex.Message);
    }

    [Fact]
    public void ValidateBill_ZeroQuantity_Throws()
    {
        var bill = MakeValidBill();
        bill.Lines[0].Quantity = 0;
        var ex = Assert.Throws<InvalidOperationException>(() => BillService.ValidateBill(bill));
        Assert.Contains("quantity", ex.Message);
    }

    [Fact]
    public void ValidateBill_NegativeUnitPrice_Throws()
    {
        var bill = MakeValidBill();
        bill.Lines[0].UnitPrice = -5;
        var ex = Assert.Throws<InvalidOperationException>(() => BillService.ValidateBill(bill));
        Assert.Contains("negative", ex.Message);
    }

    [Fact]
    public void ValidateBill_MissingAccountId_Throws()
    {
        var bill = MakeValidBill();
        bill.Lines[0].AccountId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => BillService.ValidateBill(bill));
        Assert.Contains("expense account", ex.Message);
    }

    [Fact]
    public void ValidateBill_MissingDescription_Throws()
    {
        var bill = MakeValidBill();
        bill.Lines[0].Description = "";
        var ex = Assert.Throws<InvalidOperationException>(() => BillService.ValidateBill(bill));
        Assert.Contains("description", ex.Message);
    }

    [Theory]
    [InlineData(PaymentTerms.DueOnReceipt, 0)]
    [InlineData(PaymentTerms.Net30, 30)]
    [InlineData(PaymentTerms.Net45, 45)]
    public void ComputeDueDate_ReturnsCorrectDate(PaymentTerms terms, int expectedDays)
    {
        var billDate = new DateTime(2026, 2, 1);
        var dueDate = BillService.ComputeDueDate(billDate, terms);
        Assert.Equal(billDate.AddDays(expectedDays), dueDate);
    }

    [Fact]
    public void Bill_Subtotal_CalculatedFromLines()
    {
        var bill = MakeValidBill();
        Assert.Equal(350m, bill.Subtotal);
        Assert.Equal(350m, bill.Total);
    }

    [Fact]
    public void Bill_BalanceDue_ReflectsAmountPaid()
    {
        var bill = MakeValidBill();
        bill.AmountPaid = 200;
        Assert.Equal(150m, bill.BalanceDue);
    }

    private static Bill MakeValidBill() => new()
    {
        CompanyId = "co-test",
        VendorId = "vend-1",
        VendorName = "Test Vendor",
        BillDate = new DateTime(2026, 2, 1),
        PaymentTerms = PaymentTerms.Net30,
        Lines = new()
        {
            new BillLine { Description = "Office Supplies", Quantity = 5, UnitPrice = 50, AccountId = "exp-1", AccountNumber = "6400", AccountName = "Office Supplies" },
            new BillLine { Description = "Printer Paper", Quantity = 2, UnitPrice = 50, AccountId = "exp-1", AccountNumber = "6400", AccountName = "Office Supplies" },
        }
    };
}
