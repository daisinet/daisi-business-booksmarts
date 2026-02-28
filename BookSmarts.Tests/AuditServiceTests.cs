using BookSmarts.Core.Models;
using BookSmarts.Data;
using BookSmarts.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BookSmarts.Tests;

public class AuditServiceTests
{
    private static (AuditService service, Mock<BookSmartsCosmo> cosmo) CreateSut()
    {
        var cosmo = new Mock<BookSmartsCosmo>(Mock.Of<IConfiguration>(), "Cosmo:ConnectionString");
        var service = new AuditService(cosmo.Object);
        return (service, cosmo);
    }

    [Fact]
    public async Task LogAsync_CreatesAuditEntry()
    {
        var (service, cosmo) = CreateSut();
        AuditEntry? captured = null;
        cosmo.Setup(c => c.CreateAuditEntryAsync(It.IsAny<AuditEntry>()))
            .Callback<AuditEntry>(e => captured = e)
            .ReturnsAsync((AuditEntry e) => e);

        await service.LogAsync("co-1", "acc-1", "Created", "JournalEntry", "je-1",
            "JE-000001", "Test entry created", userName: "tester");

        Assert.NotNull(captured);
        Assert.Equal("co-1", captured.CompanyId);
        Assert.Equal("acc-1", captured.AccountId);
        Assert.Equal("Created", captured.Action);
        Assert.Equal("JournalEntry", captured.EntityType);
        Assert.Equal("je-1", captured.EntityId);
        Assert.Equal("JE-000001", captured.EntityLabel);
        Assert.Equal("Test entry created", captured.Description);
        Assert.Equal("tester", captured.UserName);
    }

    [Fact]
    public async Task LogAsync_DoesNotThrowOnFailure()
    {
        var (service, cosmo) = CreateSut();
        cosmo.Setup(c => c.CreateAuditEntryAsync(It.IsAny<AuditEntry>()))
            .ThrowsAsync(new CosmosException("fail", System.Net.HttpStatusCode.InternalServerError, 0, "", 0));

        // Should not throw — audit failures are swallowed
        await service.LogAsync("co-1", "acc-1", "Created", "JournalEntry", "je-1");
    }

    [Fact]
    public async Task GetAuditLogAsync_ReturnsEntries()
    {
        var (service, cosmo) = CreateSut();
        var expected = new List<AuditEntry>
        {
            new() { id = "aud-1", CompanyId = "co-1", Action = "Created", EntityType = "JournalEntry" },
            new() { id = "aud-2", CompanyId = "co-1", Action = "Posted", EntityType = "JournalEntry" }
        };

        cosmo.Setup(c => c.GetAuditEntriesAsync("co-1", null, null, null, null, 100))
            .ReturnsAsync(expected);

        var result = await service.GetAuditLogAsync("co-1");

        Assert.Equal(2, result.Count);
    }
}
