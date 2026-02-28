using System.Security.Cryptography;
using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;
using BookSmarts.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BookSmarts.Tests;

public class ServiceEncryptionTests
{
    private static (EncryptionContext context, Mock<BookSmartsCosmo> cosmo) CreateDeps(bool encrypt = true)
    {
        var cosmo = new Mock<BookSmartsCosmo>(Mock.Of<IConfiguration>(), "Cosmo:ConnectionString");
        var context = new EncryptionContext();

        if (encrypt)
        {
            context.IsEncryptionEnabled = true;
            var adk = new byte[32];
            RandomNumberGenerator.Fill(adk);
            context.Unlock(adk);
        }

        return (context, cosmo);
    }

    [Fact]
    public async Task OrganizationService_CreateOrg_EncryptsBeforeWrite()
    {
        var (context, cosmo) = CreateDeps();

        Organization? captured = null;
        cosmo.Setup(c => c.CreateOrganizationAsync(It.IsAny<Organization>()))
             .Callback<Organization>(o => captured = new Organization
             {
                 id = o.id, AccountId = o.AccountId, Name = o.Name,
                 EncryptedPayload = o.EncryptedPayload
             })
             .ReturnsAsync((Organization o) => o);

        var service = new OrganizationService(cosmo.Object, context);
        var result = await service.CreateOrganizationAsync(new Organization
        {
            AccountId = "acc-1",
            Name = "Test Org"
        });

        // What was written to Cosmos should have encrypted payload and null name
        Assert.NotNull(captured!.EncryptedPayload);
        Assert.Null(captured.Name);

        // What was returned to caller should be decrypted
        Assert.Equal("Test Org", result.Name);
        Assert.Null(result.EncryptedPayload);
    }

    [Fact]
    public async Task OrganizationService_GetOrg_DecryptsAfterRead()
    {
        var (context, cosmo) = CreateDeps();
        var adk = context.GetAdk();

        // Prepare an encrypted org
        var org = new Organization { id = "org-1", AccountId = "acc-1", Name = "Encrypted Org" };
        FieldEncryption.EncryptFields(org, adk);

        cosmo.Setup(c => c.GetOrganizationAsync("org-1", "acc-1")).ReturnsAsync(org);

        var service = new OrganizationService(cosmo.Object, context);
        var result = await service.GetOrganizationAsync("org-1", "acc-1");

        Assert.NotNull(result);
        Assert.Equal("Encrypted Org", result.Name);
        Assert.Null(result.EncryptedPayload);
    }

    [Fact]
    public async Task ChartOfAccountsService_EncryptsOnCreate()
    {
        var (context, cosmo) = CreateDeps();

        ChartOfAccountEntry? captured = null;
        cosmo.Setup(c => c.AccountNumberExistsAsync(It.IsAny<string>(), It.IsAny<string>(), null))
             .ReturnsAsync(false);
        cosmo.Setup(c => c.CreateAccountAsync(It.IsAny<ChartOfAccountEntry>()))
             .Callback<ChartOfAccountEntry>(e => captured = new ChartOfAccountEntry
             {
                 Name = e.Name, Description = e.Description, EncryptedPayload = e.EncryptedPayload,
                 AccountNumber = e.AccountNumber
             })
             .ReturnsAsync((ChartOfAccountEntry e) => e);

        var service = new ChartOfAccountsService(cosmo.Object, context);
        var result = await service.CreateAccountEntryAsync(new ChartOfAccountEntry
        {
            CompanyId = "co-1", AccountNumber = "1000", Name = "Cash",
            Description = "Primary cash", Category = AccountCategory.Asset
        });

        Assert.NotNull(captured!.EncryptedPayload);
        Assert.Null(captured.Name);
        Assert.Equal("1000", captured.AccountNumber); // Not encrypted

        Assert.Equal("Cash", result.Name);
        Assert.Equal("Primary cash", result.Description);
    }

    [Fact]
    public async Task AccountingService_EncryptsJournalEntry()
    {
        var (context, cosmo) = CreateDeps();

        JournalEntry? captured = null;
        cosmo.Setup(c => c.GetNextEntryNumberAsync(It.IsAny<string>())).ReturnsAsync(1);
        cosmo.Setup(c => c.CreateJournalEntryAsync(It.IsAny<JournalEntry>()))
             .Callback<JournalEntry>(e => captured = new JournalEntry
             {
                 Description = e.Description, Lines = e.Lines,
                 EncryptedPayload = e.EncryptedPayload, EntryNumber = e.EntryNumber
             })
             .ReturnsAsync((JournalEntry e) => e);

        var service = new AccountingService(cosmo.Object, context);
        var result = await service.CreateJournalEntryAsync(new JournalEntry
        {
            CompanyId = "co-1",
            Description = "Office supplies",
            Lines = new List<JournalLine>
            {
                new() { AccountId = "coa-1", Debit = 100, Credit = 0 },
                new() { AccountId = "coa-2", Debit = 0, Credit = 100 }
            }
        });

        // Written to Cosmos: encrypted
        Assert.NotNull(captured!.EncryptedPayload);
        Assert.Null(captured.Description);
        Assert.Null(captured.Lines);

        // Returned to caller: decrypted
        Assert.Equal("Office supplies", result.Description);
        Assert.Equal(2, result.Lines.Count);
    }

    [Fact]
    public async Task WithoutEncryption_PassthroughWorks()
    {
        var (context, cosmo) = CreateDeps(encrypt: false);

        cosmo.Setup(c => c.CreateOrganizationAsync(It.IsAny<Organization>()))
             .ReturnsAsync((Organization o) => o);

        var service = new OrganizationService(cosmo.Object, context);
        var result = await service.CreateOrganizationAsync(new Organization
        {
            AccountId = "acc-1", Name = "Plain Org"
        });

        Assert.Equal("Plain Org", result.Name);
        Assert.Null(result.EncryptedPayload);
    }
}
