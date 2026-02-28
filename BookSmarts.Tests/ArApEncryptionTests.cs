using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;

namespace BookSmarts.Tests;

public class ArApEncryptionTests
{
    [Fact]
    public void EncryptionFieldMaps_Customer_HasExpectedFields()
    {
        var fields = EncryptionFieldMaps.GetFields<Customer>();
        Assert.Contains("Name", fields);
        Assert.Contains("Email", fields);
        Assert.Contains("Phone", fields);
        Assert.Contains("Address", fields);
        Assert.Contains("ContactPerson", fields);
        Assert.Contains("Notes", fields);
    }

    [Fact]
    public void EncryptionFieldMaps_Vendor_HasExpectedFields()
    {
        var fields = EncryptionFieldMaps.GetFields<Vendor>();
        Assert.Contains("Name", fields);
        Assert.Contains("Email", fields);
        Assert.Contains("Phone", fields);
        Assert.Contains("Address", fields);
        Assert.Contains("ContactPerson", fields);
        Assert.Contains("Notes", fields);
    }

    [Fact]
    public void EncryptionFieldMaps_Invoice_HasExpectedFields()
    {
        var fields = EncryptionFieldMaps.GetFields<Invoice>();
        Assert.Contains("CustomerName", fields);
        Assert.Contains("Lines", fields);
        Assert.Contains("Notes", fields);
        Assert.Contains("Memo", fields);
    }

    [Fact]
    public void EncryptionFieldMaps_Bill_HasExpectedFields()
    {
        var fields = EncryptionFieldMaps.GetFields<Bill>();
        Assert.Contains("VendorName", fields);
        Assert.Contains("Lines", fields);
        Assert.Contains("Notes", fields);
        Assert.Contains("Memo", fields);
        Assert.Contains("VendorReferenceNumber", fields);
    }

    [Fact]
    public void EncryptionFieldMaps_Payment_HasExpectedFields()
    {
        var fields = EncryptionFieldMaps.GetFields<Payment>();
        Assert.Contains("CustomerName", fields);
        Assert.Contains("VendorName", fields);
        Assert.Contains("Allocations", fields);
        Assert.Contains("Notes", fields);
        Assert.Contains("Amount", fields);
    }

    [Fact]
    public void Customer_ImplementsIEncryptable()
    {
        var customer = new Customer { Name = "Test" };
        Assert.IsAssignableFrom<IEncryptable>(customer);
        Assert.Null(customer.EncryptedPayload);
    }

    [Fact]
    public void Vendor_ImplementsIEncryptable()
    {
        var vendor = new Vendor { Name = "Test" };
        Assert.IsAssignableFrom<IEncryptable>(vendor);
        Assert.Null(vendor.EncryptedPayload);
    }

    [Fact]
    public void Invoice_ImplementsIEncryptable()
    {
        var invoice = new Invoice();
        Assert.IsAssignableFrom<IEncryptable>(invoice);
    }
}
