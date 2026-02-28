using System.Security.Cryptography;
using BookSmarts.Core.Encryption;

namespace BookSmarts.Services;

/// <summary>
/// Key derivation and ADK wrapping for zero-knowledge encryption.
/// </summary>
public static class KeyDerivation
{
    private const int AdkSize = 32; // 256-bit
    private const int SaltSize = 32; // 256-bit
    private const int DefaultPinIterations = 600_000;
    private const int DefaultRecoveryIterations = 600_000;

    /// <summary>
    /// Generates a random 256-bit Account Data Key.
    /// </summary>
    public static byte[] GenerateAdk()
    {
        var adk = new byte[AdkSize];
        RandomNumberGenerator.Fill(adk);
        return adk;
    }

    /// <summary>
    /// Generates a random 256-bit salt.
    /// </summary>
    public static byte[] GenerateSalt()
    {
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    /// <summary>
    /// Derives a Key Encryption Key from a secret (PIN or recovery phrase) using PBKDF2-SHA256.
    /// </summary>
    public static byte[] DeriveKek(string secret, byte[] salt, int iterations = DefaultPinIterations)
    {
        var secretBytes = System.Text.Encoding.UTF8.GetBytes(secret);
        return Rfc2898DeriveBytes.Pbkdf2(secretBytes, salt, iterations, HashAlgorithmName.SHA256, 32);
    }

    /// <summary>
    /// Wraps (encrypts) the ADK with a KEK using AES-256-GCM.
    /// </summary>
    public static string WrapAdk(byte[] adk, byte[] kek)
    {
        var encrypted = AesGcmHelper.Encrypt(adk, kek);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Unwraps (decrypts) the ADK with a KEK.
    /// </summary>
    public static byte[] UnwrapAdk(string wrappedAdk, byte[] kek)
    {
        var combined = Convert.FromBase64String(wrappedAdk);
        return AesGcmHelper.Decrypt(combined, kek);
    }

    /// <summary>
    /// Generates a 12-word BIP39 mnemonic recovery phrase (128 bits entropy).
    /// </summary>
    public static string GenerateMnemonic()
    {
        // 128 bits of entropy = 12 words (128 / 11 bits per word index + 4 checksum bits)
        var entropy = new byte[16]; // 128 bits
        RandomNumberGenerator.Fill(entropy);

        // Compute SHA-256 checksum
        var hash = SHA256.HashData(entropy);
        var checksumBits = 4; // 128 / 32

        // Convert entropy + checksum to bit string
        var bits = new bool[128 + checksumBits];
        for (int i = 0; i < 128; i++)
            bits[i] = (entropy[i / 8] & (1 << (7 - (i % 8)))) != 0;
        for (int i = 0; i < checksumBits; i++)
            bits[128 + i] = (hash[0] & (1 << (7 - i))) != 0;

        // Split into 12 groups of 11 bits
        var words = new string[12];
        for (int i = 0; i < 12; i++)
        {
            int index = 0;
            for (int j = 0; j < 11; j++)
            {
                if (bits[i * 11 + j])
                    index |= 1 << (10 - j);
            }
            words[i] = Bip39Words.Words[index];
        }

        return string.Join(" ", words);
    }

    public static int GetDefaultPinIterations() => DefaultPinIterations;
    public static int GetDefaultRecoveryIterations() => DefaultRecoveryIterations;
}
