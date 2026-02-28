using System.Security.Cryptography;

namespace BookSmarts.Services;

/// <summary>
/// Scoped service (one per Blazor circuit) that holds the decrypted ADK in memory.
/// The ADK is only available after the user enters their PIN.
/// </summary>
public class EncryptionContext : IDisposable
{
    private byte[]? _adk;

    /// <summary>
    /// Whether the account has encryption enabled.
    /// </summary>
    public bool IsEncryptionEnabled { get; set; }

    /// <summary>
    /// Whether the user has unlocked encryption this session (entered PIN).
    /// </summary>
    public bool IsUnlocked => _adk != null;

    /// <summary>
    /// Gets the ADK for encrypt/decrypt operations. Throws if locked.
    /// </summary>
    public byte[] GetAdk()
    {
        return _adk ?? throw new InvalidOperationException(
            "Encryption is locked. Please enter your PIN to unlock.");
    }

    /// <summary>
    /// Returns the ADK if unlocked, or null if locked/disabled.
    /// Use this when encryption is optional (backwards compatibility).
    /// </summary>
    public byte[]? GetAdkOrNull() => _adk;

    /// <summary>
    /// Unlocks encryption by storing the decrypted ADK in memory.
    /// </summary>
    public void Unlock(byte[] adk)
    {
        _adk = (byte[])adk.Clone();
    }

    /// <summary>
    /// Locks encryption by clearing the ADK from memory.
    /// </summary>
    public void Lock()
    {
        if (_adk != null)
        {
            CryptographicOperations.ZeroMemory(_adk);
            _adk = null;
        }
    }

    public void Dispose()
    {
        Lock();
    }
}
