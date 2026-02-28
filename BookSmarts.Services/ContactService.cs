using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class ContactService(BookSmartsCosmo cosmo, EncryptionContext encryption)
{
    // ── Customer ──

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new InvalidOperationException("Customer name is required.");
        if (string.IsNullOrWhiteSpace(customer.CompanyId))
            throw new InvalidOperationException("Company ID is required.");

        Encrypt(customer);
        return Decrypt(await cosmo.CreateCustomerAsync(customer));
    }

    public async Task<Customer?> GetCustomerAsync(string id, string companyId)
    {
        var customer = await cosmo.GetCustomerAsync(id, companyId);
        return customer != null ? Decrypt(customer) : null;
    }

    public async Task<List<Customer>> GetCustomersAsync(string companyId, bool activeOnly = false)
    {
        var customers = await cosmo.GetCustomersAsync(companyId, activeOnly);
        return DecryptAll(customers);
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new InvalidOperationException("Customer name is required.");

        Encrypt(customer);
        return Decrypt(await cosmo.UpdateCustomerAsync(customer));
    }

    public async Task DeleteCustomerAsync(string id, string companyId)
    {
        await cosmo.DeleteCustomerAsync(id, companyId);
    }

    // ── Vendor ──

    public async Task<Vendor> CreateVendorAsync(Vendor vendor)
    {
        if (string.IsNullOrWhiteSpace(vendor.Name))
            throw new InvalidOperationException("Vendor name is required.");
        if (string.IsNullOrWhiteSpace(vendor.CompanyId))
            throw new InvalidOperationException("Company ID is required.");

        Encrypt(vendor);
        return Decrypt(await cosmo.CreateVendorAsync(vendor));
    }

    public async Task<Vendor?> GetVendorAsync(string id, string companyId)
    {
        var vendor = await cosmo.GetVendorAsync(id, companyId);
        return vendor != null ? Decrypt(vendor) : null;
    }

    public async Task<List<Vendor>> GetVendorsAsync(string companyId, bool activeOnly = false)
    {
        var vendors = await cosmo.GetVendorsAsync(companyId, activeOnly);
        return DecryptAll(vendors);
    }

    public async Task<Vendor> UpdateVendorAsync(Vendor vendor)
    {
        if (string.IsNullOrWhiteSpace(vendor.Name))
            throw new InvalidOperationException("Vendor name is required.");

        Encrypt(vendor);
        return Decrypt(await cosmo.UpdateVendorAsync(vendor));
    }

    public async Task DeleteVendorAsync(string id, string companyId)
    {
        await cosmo.DeleteVendorAsync(id, companyId);
    }

    // ── Encryption helpers ──

    private void Encrypt<T>(T model) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.EncryptFields(model, adk);
    }

    private T Decrypt<T>(T model) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.DecryptFields(model, adk);
        return model;
    }

    private List<T> DecryptAll<T>(List<T> models) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.DecryptAll(models, adk);
        return models;
    }
}
