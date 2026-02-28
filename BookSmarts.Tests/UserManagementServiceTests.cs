using System.Security.Cryptography;
using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;
using BookSmarts.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BookSmarts.Tests;

public class UserManagementServiceTests
{
    private static (UserManagementService service, Mock<BookSmartsCosmo> cosmo) CreateSut(bool encrypt = false)
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

        var service = new UserManagementService(cosmo.Object, context);
        return (service, cosmo);
    }

    [Fact]
    public async Task CreateUserAsync_CreatesUser()
    {
        var (service, cosmo) = CreateSut();
        BookSmartsUser? captured = null;
        cosmo.Setup(c => c.CreateBookSmartsUserAsync(It.IsAny<BookSmartsUser>()))
            .Callback<BookSmartsUser>(u => captured = u)
            .ReturnsAsync((BookSmartsUser u) => u);

        var user = new BookSmartsUser
        {
            AccountId = "acc-1",
            DaisinetUserId = "du-1",
            Name = "Alice",
            Email = "alice@example.com",
            Role = BookSmartsRole.Owner
        };

        var result = await service.CreateUserAsync(user);

        Assert.NotNull(captured);
        Assert.Equal("acc-1", result.AccountId);
        Assert.Equal("du-1", result.DaisinetUserId);
        Assert.Equal(BookSmartsRole.Owner, result.Role);
    }

    [Fact]
    public async Task GetUserByDaisinetIdAsync_ReturnsUser()
    {
        var (service, cosmo) = CreateSut();
        var existing = new BookSmartsUser
        {
            id = "bsu-1",
            AccountId = "acc-1",
            DaisinetUserId = "du-1",
            Name = "Alice",
            Role = BookSmartsRole.Accountant
        };

        cosmo.Setup(c => c.GetBookSmartsUserByDaisinetIdAsync("du-1", "acc-1"))
            .ReturnsAsync(existing);

        var result = await service.GetUserByDaisinetIdAsync("du-1", "acc-1");

        Assert.NotNull(result);
        Assert.Equal("bsu-1", result!.id);
        Assert.Equal(BookSmartsRole.Accountant, result.Role);
    }

    [Fact]
    public async Task GetUserByDaisinetIdAsync_ReturnsNullWhenNotFound()
    {
        var (service, cosmo) = CreateSut();
        cosmo.Setup(c => c.GetBookSmartsUserByDaisinetIdAsync("du-99", "acc-1"))
            .ReturnsAsync((BookSmartsUser?)null);

        var result = await service.GetUserByDaisinetIdAsync("du-99", "acc-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsAllUsers()
    {
        var (service, cosmo) = CreateSut();
        var users = new List<BookSmartsUser>
        {
            new() { id = "bsu-1", AccountId = "acc-1", Name = "Alice", Role = BookSmartsRole.Owner },
            new() { id = "bsu-2", AccountId = "acc-1", Name = "Bob", Role = BookSmartsRole.Viewer }
        };
        cosmo.Setup(c => c.GetBookSmartsUsersAsync("acc-1", false)).ReturnsAsync(users);

        var result = await service.GetUsersAsync("acc-1");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DeactivateUserAsync_DeactivatesNonOwner()
    {
        var (service, cosmo) = CreateSut();
        var user = new BookSmartsUser
        {
            id = "bsu-2",
            AccountId = "acc-1",
            DaisinetUserId = "du-2",
            Name = "Bob",
            Role = BookSmartsRole.Bookkeeper,
            IsActive = true
        };

        cosmo.Setup(c => c.GetBookSmartsUserAsync("bsu-2", "acc-1")).ReturnsAsync(user);
        cosmo.Setup(c => c.UpdateBookSmartsUserAsync(It.IsAny<BookSmartsUser>()))
            .ReturnsAsync((BookSmartsUser u) => u);

        var result = await service.DeactivateUserAsync("bsu-2", "acc-1");

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task DeactivateUserAsync_BlocksLastOwner()
    {
        var (service, cosmo) = CreateSut();
        var owner = new BookSmartsUser
        {
            id = "bsu-1",
            AccountId = "acc-1",
            Name = "Alice",
            Role = BookSmartsRole.Owner,
            IsActive = true
        };

        cosmo.Setup(c => c.GetBookSmartsUserAsync("bsu-1", "acc-1")).ReturnsAsync(owner);
        cosmo.Setup(c => c.GetBookSmartsUsersAsync("acc-1", true))
            .ReturnsAsync(new List<BookSmartsUser> { owner });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeactivateUserAsync("bsu-1", "acc-1"));
    }

    [Fact]
    public async Task DeactivateUserAsync_AllowsOwnerWhenOtherOwnersExist()
    {
        var (service, cosmo) = CreateSut();
        var owner1 = new BookSmartsUser
        {
            id = "bsu-1",
            AccountId = "acc-1",
            Name = "Alice",
            Role = BookSmartsRole.Owner,
            IsActive = true
        };
        var owner2 = new BookSmartsUser
        {
            id = "bsu-2",
            AccountId = "acc-1",
            Name = "Bob",
            Role = BookSmartsRole.Owner,
            IsActive = true
        };

        cosmo.Setup(c => c.GetBookSmartsUserAsync("bsu-1", "acc-1")).ReturnsAsync(owner1);
        cosmo.Setup(c => c.GetBookSmartsUsersAsync("acc-1", true))
            .ReturnsAsync(new List<BookSmartsUser> { owner1, owner2 });
        cosmo.Setup(c => c.UpdateBookSmartsUserAsync(It.IsAny<BookSmartsUser>()))
            .ReturnsAsync((BookSmartsUser u) => u);

        var result = await service.DeactivateUserAsync("bsu-1", "acc-1");

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task ReactivateUserAsync_ReactivatesUser()
    {
        var (service, cosmo) = CreateSut();
        var user = new BookSmartsUser
        {
            id = "bsu-2",
            AccountId = "acc-1",
            Name = "Bob",
            IsActive = false
        };

        cosmo.Setup(c => c.GetBookSmartsUserAsync("bsu-2", "acc-1")).ReturnsAsync(user);
        cosmo.Setup(c => c.UpdateBookSmartsUserAsync(It.IsAny<BookSmartsUser>()))
            .ReturnsAsync((BookSmartsUser u) => u);

        var result = await service.ReactivateUserAsync("bsu-2", "acc-1");

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task DeactivateUserAsync_ThrowsWhenUserNotFound()
    {
        var (service, cosmo) = CreateSut();
        cosmo.Setup(c => c.GetBookSmartsUserAsync("bsu-99", "acc-1"))
            .ReturnsAsync((BookSmartsUser?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeactivateUserAsync("bsu-99", "acc-1"));
    }

    [Fact]
    public async Task CreateUserAsync_EncryptsNameAndEmail()
    {
        var (service, cosmo) = CreateSut(encrypt: true);
        BookSmartsUser? captured = null;
        cosmo.Setup(c => c.CreateBookSmartsUserAsync(It.IsAny<BookSmartsUser>()))
            .Callback<BookSmartsUser>(u => captured = new BookSmartsUser
            {
                id = u.id,
                Name = u.Name,
                Email = u.Email,
                EncryptedPayload = u.EncryptedPayload
            })
            .ReturnsAsync((BookSmartsUser u) => u);

        await service.CreateUserAsync(new BookSmartsUser
        {
            AccountId = "acc-1",
            DaisinetUserId = "du-1",
            Name = "Secret Name",
            Email = "secret@example.com",
            Role = BookSmartsRole.Owner
        });

        Assert.NotNull(captured);
        Assert.NotNull(captured!.EncryptedPayload);
        // Encrypted fields should be cleared after encryption
        Assert.Null(captured.Name);
        Assert.Null(captured.Email);
    }

    // ── Permission Service Tests ──

    [Fact]
    public void PermissionService_OwnerCanManageUsers()
    {
        Assert.True(PermissionService.CanManageUsers(BookSmartsRole.Owner));
        Assert.False(PermissionService.CanManageUsers(BookSmartsRole.Accountant));
        Assert.False(PermissionService.CanManageUsers(BookSmartsRole.Bookkeeper));
        Assert.False(PermissionService.CanManageUsers(BookSmartsRole.Viewer));
    }

    [Fact]
    public void PermissionService_BookkeeperCanWriteJournals()
    {
        Assert.True(PermissionService.CanWriteJournals(BookSmartsRole.Owner));
        Assert.True(PermissionService.CanWriteJournals(BookSmartsRole.Accountant));
        Assert.True(PermissionService.CanWriteJournals(BookSmartsRole.Bookkeeper));
        Assert.False(PermissionService.CanWriteJournals(BookSmartsRole.Viewer));
    }

    [Fact]
    public void PermissionService_AccountantCanWriteBudgets()
    {
        Assert.True(PermissionService.CanWriteBudgets(BookSmartsRole.Owner));
        Assert.True(PermissionService.CanWriteBudgets(BookSmartsRole.Accountant));
        Assert.False(PermissionService.CanWriteBudgets(BookSmartsRole.Bookkeeper));
        Assert.False(PermissionService.CanWriteBudgets(BookSmartsRole.Viewer));
    }

    [Fact]
    public void PermissionService_ViewerCannotUseAi()
    {
        Assert.True(PermissionService.CanUseAiFeatures(BookSmartsRole.Owner));
        Assert.True(PermissionService.CanUseAiFeatures(BookSmartsRole.Accountant));
        Assert.True(PermissionService.CanUseAiFeatures(BookSmartsRole.Bookkeeper));
        Assert.False(PermissionService.CanUseAiFeatures(BookSmartsRole.Viewer));
    }

    [Fact]
    public void PermissionService_OwnerWithEmptyCompanyIds_HasAccessToAll()
    {
        var owner = new BookSmartsUser
        {
            Role = BookSmartsRole.Owner,
            CompanyIds = new List<string>()
        };

        Assert.True(PermissionService.HasCompanyAccess(owner, "co-1"));
        Assert.True(PermissionService.HasCompanyAccess(owner, "co-99"));
    }

    [Fact]
    public void PermissionService_NonOwner_RestrictedToAssignedCompanies()
    {
        var bookkeeper = new BookSmartsUser
        {
            Role = BookSmartsRole.Bookkeeper,
            CompanyIds = new List<string> { "co-1", "co-2" }
        };

        Assert.True(PermissionService.HasCompanyAccess(bookkeeper, "co-1"));
        Assert.True(PermissionService.HasCompanyAccess(bookkeeper, "co-2"));
        Assert.False(PermissionService.HasCompanyAccess(bookkeeper, "co-3"));
    }

    [Fact]
    public void PermissionService_FilterCompanies_FiltersCorrectly()
    {
        var accountant = new BookSmartsUser
        {
            Role = BookSmartsRole.Accountant,
            CompanyIds = new List<string> { "co-1" }
        };

        var allCompanies = new List<Company>
        {
            new() { id = "co-1", Name = "Company A" },
            new() { id = "co-2", Name = "Company B" },
            new() { id = "co-3", Name = "Company C" }
        };

        var filtered = PermissionService.FilterCompanies(accountant, allCompanies);

        Assert.Single(filtered);
        Assert.Equal("co-1", filtered[0].id);
    }

    [Fact]
    public void PermissionService_FilterCompanies_OwnerGetsAll()
    {
        var owner = new BookSmartsUser
        {
            Role = BookSmartsRole.Owner,
            CompanyIds = new List<string>()
        };

        var allCompanies = new List<Company>
        {
            new() { id = "co-1", Name = "Company A" },
            new() { id = "co-2", Name = "Company B" }
        };

        var filtered = PermissionService.FilterCompanies(owner, allCompanies);

        Assert.Equal(2, filtered.Count);
    }
}
