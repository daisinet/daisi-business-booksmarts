using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class PaymentValidationTests
{
    [Fact]
    public void ValidatePayment_ValidCustomerPayment_Succeeds()
    {
        var payment = MakeValidCustomerPayment();
        var ex = Record.Exception(() => PaymentService.ValidatePayment(payment, PaymentType.CustomerPayment));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidatePayment_MissingCompanyId_Throws()
    {
        var payment = MakeValidCustomerPayment();
        payment.CompanyId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => PaymentService.ValidatePayment(payment, PaymentType.CustomerPayment));
        Assert.Contains("Company ID", ex.Message);
    }

    [Fact]
    public void ValidatePayment_ZeroAmount_Throws()
    {
        var payment = MakeValidCustomerPayment();
        payment.Amount = 0;
        var ex = Assert.Throws<InvalidOperationException>(() => PaymentService.ValidatePayment(payment, PaymentType.CustomerPayment));
        Assert.Contains("greater than zero", ex.Message);
    }

    [Fact]
    public void ValidatePayment_MissingBankAccount_Throws()
    {
        var payment = MakeValidCustomerPayment();
        payment.BankAccountId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => PaymentService.ValidatePayment(payment, PaymentType.CustomerPayment));
        Assert.Contains("Bank account", ex.Message);
    }

    [Fact]
    public void ValidatePayment_CustomerPaymentMissingCustomerId_Throws()
    {
        var payment = MakeValidCustomerPayment();
        payment.CustomerId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => PaymentService.ValidatePayment(payment, PaymentType.CustomerPayment));
        Assert.Contains("Customer", ex.Message);
    }

    [Fact]
    public void ValidatePayment_VendorPaymentMissingVendorId_Throws()
    {
        var payment = MakeValidVendorPayment();
        payment.VendorId = "";
        var ex = Assert.Throws<InvalidOperationException>(() => PaymentService.ValidatePayment(payment, PaymentType.VendorPayment));
        Assert.Contains("Vendor", ex.Message);
    }

    [Fact]
    public void ValidatePayment_AllocationsExceedAmount_Throws()
    {
        var payment = MakeValidCustomerPayment();
        payment.Amount = 100;
        payment.Allocations = new()
        {
            new PaymentAllocation { InvoiceId = "inv-1", Amount = 80 },
            new PaymentAllocation { InvoiceId = "inv-2", Amount = 30 }
        };
        var ex = Assert.Throws<InvalidOperationException>(() => PaymentService.ValidatePayment(payment, PaymentType.CustomerPayment));
        Assert.Contains("exceed", ex.Message);
    }

    [Fact]
    public void Payment_UnappliedAmount_Calculated()
    {
        var payment = MakeValidCustomerPayment();
        payment.Amount = 500;
        payment.Allocations = new()
        {
            new PaymentAllocation { InvoiceId = "inv-1", Amount = 200 },
            new PaymentAllocation { InvoiceId = "inv-2", Amount = 150 }
        };
        Assert.Equal(350m, payment.AllocatedAmount);
        Assert.Equal(150m, payment.UnappliedAmount);
    }

    private static Payment MakeValidCustomerPayment() => new()
    {
        CompanyId = "co-test",
        CustomerId = "cust-1",
        CustomerName = "Test Customer",
        Amount = 500,
        BankAccountId = "bank-1",
        BankAccountNumber = "1010",
        BankAccountName = "Checking",
        PaymentDate = DateTime.Today,
        Allocations = new()
        {
            new PaymentAllocation { InvoiceId = "inv-1", InvoiceNumber = "INV-000001", Amount = 500 }
        }
    };

    private static Payment MakeValidVendorPayment() => new()
    {
        CompanyId = "co-test",
        VendorId = "vend-1",
        VendorName = "Test Vendor",
        Amount = 350,
        BankAccountId = "bank-1",
        BankAccountNumber = "1010",
        BankAccountName = "Checking",
        PaymentDate = DateTime.Today,
        Allocations = new()
        {
            new PaymentAllocation { BillId = "bl-1", BillNumber = "BILL-000001", Amount = 350 }
        }
    };
}
