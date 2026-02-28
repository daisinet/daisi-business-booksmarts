using System.Security.Cryptography;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

/// <summary>
/// Manages the encryption lifecycle: enable, unlock, reset PIN, change PIN, disable.
/// </summary>
public class EncryptionSetupService(BookSmartsCosmo cosmo, EncryptionContext encryptionContext)
{
    /// <summary>
    /// Enables encryption for an account. Generates ADK, wraps with PIN and recovery phrase.
    /// Returns the 12-word recovery phrase (must be shown to user once, never stored).
    /// </summary>
    public async Task<string> EnableEncryptionAsync(string accountId, string pin)
    {
        var existing = await cosmo.GetEncryptionConfigAsync(accountId);
        if (existing is { IsEnabled: true })
            throw new InvalidOperationException("Encryption is already enabled for this account.");

        var adk = KeyDerivation.GenerateAdk();
        var mnemonic = KeyDerivation.GenerateMnemonic();

        // Wrap ADK with PIN-derived KEK
        var pinSalt = KeyDerivation.GenerateSalt();
        var pinIterations = KeyDerivation.GetDefaultPinIterations();
        var pinKek = KeyDerivation.DeriveKek(pin, pinSalt, pinIterations);
        var pinWrapped = KeyDerivation.WrapAdk(adk, pinKek);

        // Wrap ADK with recovery-phrase-derived KEK
        var recoverySalt = KeyDerivation.GenerateSalt();
        var recoveryIterations = KeyDerivation.GetDefaultRecoveryIterations();
        var recoveryKek = KeyDerivation.DeriveKek(mnemonic, recoverySalt, recoveryIterations);
        var recoveryWrapped = KeyDerivation.WrapAdk(adk, recoveryKek);

        var config = new EncryptionConfig
        {
            AccountId = accountId,
            PinSalt = Convert.ToBase64String(pinSalt),
            PinEncryptedAdk = pinWrapped,
            PinIterations = pinIterations,
            RecoverySalt = Convert.ToBase64String(recoverySalt),
            RecoveryEncryptedAdk = recoveryWrapped,
            RecoveryIterations = recoveryIterations,
            IsEnabled = true,
            Version = 1
        };

        if (existing != null)
        {
            config.id = existing.id;
            await cosmo.UpdateEncryptionConfigAsync(config);
        }
        else
        {
            await cosmo.CreateEncryptionConfigAsync(config);
        }

        // Unlock immediately after enabling
        encryptionContext.IsEncryptionEnabled = true;
        encryptionContext.Unlock(adk);

        // Clean up key material
        CryptographicOperations.ZeroMemory(adk);
        CryptographicOperations.ZeroMemory(pinKek);
        CryptographicOperations.ZeroMemory(recoveryKek);

        return mnemonic;
    }

    /// <summary>
    /// Unlocks encryption by deriving the KEK from the PIN and unwrapping the ADK.
    /// </summary>
    public async Task<bool> UnlockWithPinAsync(string accountId, string pin)
    {
        var config = await cosmo.GetEncryptionConfigAsync(accountId);
        if (config is not { IsEnabled: true })
            return false;

        try
        {
            var pinSalt = Convert.FromBase64String(config.PinSalt);
            var kek = KeyDerivation.DeriveKek(pin, pinSalt, config.PinIterations);
            var adk = KeyDerivation.UnwrapAdk(config.PinEncryptedAdk, kek);

            encryptionContext.IsEncryptionEnabled = true;
            encryptionContext.Unlock(adk);

            CryptographicOperations.ZeroMemory(kek);
            CryptographicOperations.ZeroMemory(adk);
            return true;
        }
        catch (CryptographicException)
        {
            return false; // Wrong PIN
        }
    }

    /// <summary>
    /// Resets the PIN using the recovery phrase.
    /// </summary>
    public async Task<bool> ResetPinAsync(string accountId, string mnemonic, string newPin)
    {
        var config = await cosmo.GetEncryptionConfigAsync(accountId);
        if (config is not { IsEnabled: true })
            return false;

        try
        {
            // Unwrap ADK with recovery phrase
            var recoverySalt = Convert.FromBase64String(config.RecoverySalt);
            var recoveryKek = KeyDerivation.DeriveKek(mnemonic, recoverySalt, config.RecoveryIterations);
            var adk = KeyDerivation.UnwrapAdk(config.RecoveryEncryptedAdk, recoveryKek);

            // Re-wrap with new PIN
            var newPinSalt = KeyDerivation.GenerateSalt();
            var pinIterations = KeyDerivation.GetDefaultPinIterations();
            var newPinKek = KeyDerivation.DeriveKek(newPin, newPinSalt, pinIterations);
            var newPinWrapped = KeyDerivation.WrapAdk(adk, newPinKek);

            config.PinSalt = Convert.ToBase64String(newPinSalt);
            config.PinEncryptedAdk = newPinWrapped;
            config.PinIterations = pinIterations;
            await cosmo.UpdateEncryptionConfigAsync(config);

            // Unlock with the ADK
            encryptionContext.IsEncryptionEnabled = true;
            encryptionContext.Unlock(adk);

            CryptographicOperations.ZeroMemory(adk);
            CryptographicOperations.ZeroMemory(recoveryKek);
            CryptographicOperations.ZeroMemory(newPinKek);
            return true;
        }
        catch (CryptographicException)
        {
            return false; // Wrong recovery phrase
        }
    }

    /// <summary>
    /// Changes the PIN (requires encryption to be unlocked).
    /// </summary>
    public async Task ChangePinAsync(string accountId, string newPin)
    {
        if (!encryptionContext.IsUnlocked)
            throw new InvalidOperationException("Encryption must be unlocked to change PIN.");

        var config = await cosmo.GetEncryptionConfigAsync(accountId)
            ?? throw new InvalidOperationException("Encryption config not found.");

        var adk = encryptionContext.GetAdk();
        var newPinSalt = KeyDerivation.GenerateSalt();
        var pinIterations = KeyDerivation.GetDefaultPinIterations();
        var newPinKek = KeyDerivation.DeriveKek(newPin, newPinSalt, pinIterations);
        var newPinWrapped = KeyDerivation.WrapAdk(adk, newPinKek);

        config.PinSalt = Convert.ToBase64String(newPinSalt);
        config.PinEncryptedAdk = newPinWrapped;
        config.PinIterations = pinIterations;
        await cosmo.UpdateEncryptionConfigAsync(config);

        CryptographicOperations.ZeroMemory(newPinKek);
    }

    /// <summary>
    /// Checks whether encryption is enabled for an account and updates the context.
    /// </summary>
    public async Task<EncryptionConfig?> GetConfigAsync(string accountId)
    {
        var config = await cosmo.GetEncryptionConfigAsync(accountId);
        encryptionContext.IsEncryptionEnabled = config is { IsEnabled: true };
        return config;
    }

    /// <summary>
    /// Disables encryption. Caller must run DecryptAllDataAsync first to restore plaintext.
    /// </summary>
    public async Task DisableEncryptionAsync(string accountId)
    {
        var config = await cosmo.GetEncryptionConfigAsync(accountId);
        if (config == null) return;

        await cosmo.DeleteEncryptionConfigAsync(config.id, accountId);
        encryptionContext.IsEncryptionEnabled = false;
        encryptionContext.Lock();
    }
}
