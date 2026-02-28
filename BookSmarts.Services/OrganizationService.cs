using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class OrganizationService(BookSmartsCosmo cosmo, EncryptionContext encryption)
{
    public async Task<Organization> CreateOrganizationAsync(Organization org)
    {
        Encrypt(org);
        return Decrypt(await cosmo.CreateOrganizationAsync(org));
    }

    public async Task<Organization?> GetOrganizationAsync(string id, string accountId)
    {
        var org = await cosmo.GetOrganizationAsync(id, accountId);
        return org != null ? Decrypt(org) : null;
    }

    public async Task<List<Organization>> GetOrganizationsAsync(string accountId)
    {
        var orgs = await cosmo.GetOrganizationsAsync(accountId);
        return DecryptAll(orgs);
    }

    public async Task<Organization> UpdateOrganizationAsync(Organization org)
    {
        Encrypt(org);
        return Decrypt(await cosmo.UpdateOrganizationAsync(org));
    }

    public async Task<Company> CreateCompanyAsync(Company company)
    {
        Encrypt(company);
        return Decrypt(await cosmo.CreateCompanyAsync(company));
    }

    public async Task<Company?> GetCompanyAsync(string id, string accountId)
    {
        var company = await cosmo.GetCompanyAsync(id, accountId);
        return company != null ? Decrypt(company) : null;
    }

    public async Task<List<Company>> GetCompaniesAsync(string accountId, string? organizationId = null)
    {
        var companies = await cosmo.GetCompaniesAsync(accountId, organizationId);
        return DecryptAll(companies);
    }

    public async Task<Company> UpdateCompanyAsync(Company company)
    {
        Encrypt(company);
        return Decrypt(await cosmo.UpdateCompanyAsync(company));
    }

    public async Task<Division> CreateDivisionAsync(Division division)
    {
        Encrypt(division);
        return Decrypt(await cosmo.CreateDivisionAsync(division));
    }

    public async Task<List<Division>> GetDivisionsAsync(string accountId, string organizationId)
    {
        var divisions = await cosmo.GetDivisionsAsync(accountId, organizationId);
        return DecryptAll(divisions);
    }

    public async Task<Division> UpdateDivisionAsync(Division division)
    {
        Encrypt(division);
        return Decrypt(await cosmo.UpdateDivisionAsync(division));
    }

    public Task DeleteDivisionAsync(string id, string accountId)
        => cosmo.DeleteDivisionAsync(id, accountId);

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
