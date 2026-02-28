using System.Security.Cryptography;

namespace BookSmarts.Services;

/// <summary>
/// AES-256-GCM encryption for Plaid access tokens.
/// Wire format: base64(nonce[12] + ciphertext[n] + tag[16])
/// </summary>
public static class TokenEncryption
{
    public static string Encrypt(string plaintext, string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        return AesGcmHelper.EncryptString(plaintext, key);
    }

    public static string Decrypt(string encrypted, string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        return AesGcmHelper.DecryptString(encrypted, key);
    }

    /// <summary>
    /// Generates a new 256-bit key suitable for AES-256-GCM.
    /// </summary>
    public static string GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
