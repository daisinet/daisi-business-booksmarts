using System.Security.Cryptography;
using BookSmarts.Core.Models;
using BookSmarts.Data;
using BookSmarts.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BookSmarts.Tests;

public class EncryptionSetupTests
{
    private static (EncryptionSetupService service, EncryptionContext context, Mock<BookSmartsCosmo> cosmo) CreateService()
    {
        var cosmo = new Mock<BookSmartsCosmo>(Mock.Of<IConfiguration>(), "Cosmo:ConnectionString");
        var context = new EncryptionContext();
        var service = new EncryptionSetupService(cosmo.Object, context);
        return (service, context, cosmo);
    }

    [Fact]
    public async Task EnableEncryption_ReturnsRecoveryPhrase_UnlocksContext()
    {
        var (service, context, cosmo) = CreateService();

        cosmo.Setup(c => c.GetEncryptionConfigAsync("acc-1")).ReturnsAsync((EncryptionConfig?)null);
        cosmo.Setup(c => c.CreateEncryptionConfigAsync(It.IsAny<EncryptionConfig>()))
             .ReturnsAsync((EncryptionConfig cfg) => cfg);

        var mnemonic = await service.EnableEncryptionAsync("acc-1", "123456");

        Assert.Equal(12, mnemonic.Split(' ').Length);
        Assert.True(context.IsEncryptionEnabled);
        Assert.True(context.IsUnlocked);
        Assert.Equal(32, context.GetAdk().Length);
    }

    [Fact]
    public async Task UnlockWithCorrectPin_ReturnsTrue()
    {
        var (service, context, cosmo) = CreateService();

        // First enable encryption to get a valid config
        EncryptionConfig? savedConfig = null;
        cosmo.Setup(c => c.GetEncryptionConfigAsync("acc-1")).ReturnsAsync(() => savedConfig);
        cosmo.Setup(c => c.CreateEncryptionConfigAsync(It.IsAny<EncryptionConfig>()))
             .Callback<EncryptionConfig>(cfg => savedConfig = cfg)
             .ReturnsAsync((EncryptionConfig cfg) => cfg);

        await service.EnableEncryptionAsync("acc-1", "123456");

        // Reset context to simulate new session
        context.Lock();
        context.IsEncryptionEnabled = false;

        var result = await service.UnlockWithPinAsync("acc-1", "123456");
        Assert.True(result);
        Assert.True(context.IsUnlocked);
    }

    [Fact]
    public async Task UnlockWithWrongPin_ReturnsFalse()
    {
        var (service, context, cosmo) = CreateService();

        EncryptionConfig? savedConfig = null;
        cosmo.Setup(c => c.GetEncryptionConfigAsync("acc-1")).ReturnsAsync(() => savedConfig);
        cosmo.Setup(c => c.CreateEncryptionConfigAsync(It.IsAny<EncryptionConfig>()))
             .Callback<EncryptionConfig>(cfg => savedConfig = cfg)
             .ReturnsAsync((EncryptionConfig cfg) => cfg);

        await service.EnableEncryptionAsync("acc-1", "123456");
        context.Lock();
        context.IsEncryptionEnabled = false;

        var result = await service.UnlockWithPinAsync("acc-1", "999999");
        Assert.False(result);
        Assert.False(context.IsUnlocked);
    }

    [Fact]
    public async Task ResetPin_WithCorrectMnemonic_Works()
    {
        var (service, context, cosmo) = CreateService();

        EncryptionConfig? savedConfig = null;
        cosmo.Setup(c => c.GetEncryptionConfigAsync("acc-1")).ReturnsAsync(() => savedConfig);
        cosmo.Setup(c => c.CreateEncryptionConfigAsync(It.IsAny<EncryptionConfig>()))
             .Callback<EncryptionConfig>(cfg => savedConfig = cfg)
             .ReturnsAsync((EncryptionConfig cfg) => cfg);
        cosmo.Setup(c => c.UpdateEncryptionConfigAsync(It.IsAny<EncryptionConfig>()))
             .Callback<EncryptionConfig>(cfg => savedConfig = cfg)
             .ReturnsAsync((EncryptionConfig cfg) => cfg);

        var mnemonic = await service.EnableEncryptionAsync("acc-1", "123456");

        // Reset PIN with mnemonic
        context.Lock();
        context.IsEncryptionEnabled = false;

        var result = await service.ResetPinAsync("acc-1", mnemonic, "654321");
        Assert.True(result);
        Assert.True(context.IsUnlocked);

        // Verify new PIN works
        context.Lock();
        context.IsEncryptionEnabled = false;

        var unlockResult = await service.UnlockWithPinAsync("acc-1", "654321");
        Assert.True(unlockResult);
    }

    [Fact]
    public async Task ChangePin_WhenUnlocked_Works()
    {
        var (service, context, cosmo) = CreateService();

        EncryptionConfig? savedConfig = null;
        cosmo.Setup(c => c.GetEncryptionConfigAsync("acc-1")).ReturnsAsync(() => savedConfig);
        cosmo.Setup(c => c.CreateEncryptionConfigAsync(It.IsAny<EncryptionConfig>()))
             .Callback<EncryptionConfig>(cfg => savedConfig = cfg)
             .ReturnsAsync((EncryptionConfig cfg) => cfg);
        cosmo.Setup(c => c.UpdateEncryptionConfigAsync(It.IsAny<EncryptionConfig>()))
             .Callback<EncryptionConfig>(cfg => savedConfig = cfg)
             .ReturnsAsync((EncryptionConfig cfg) => cfg);

        await service.EnableEncryptionAsync("acc-1", "123456");

        // Change PIN while unlocked
        await service.ChangePinAsync("acc-1", "111111");

        // Verify new PIN works
        context.Lock();
        context.IsEncryptionEnabled = false;

        Assert.True(await service.UnlockWithPinAsync("acc-1", "111111"));
        Assert.False(await service.UnlockWithPinAsync("acc-1", "123456")); // Old PIN should fail
    }
}
