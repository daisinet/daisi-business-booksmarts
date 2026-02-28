using System.Security.Cryptography;

namespace BookSmarts.Services;

/// <summary>
/// Low-level AES-256-GCM encrypt/decrypt operations.
/// Wire format: base64(nonce[12] + ciphertext[n] + tag[16])
/// </summary>
public static class AesGcmHelper
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);

        return result;
    }

    public static byte[] Decrypt(byte[] combined, byte[] key)
    {
        if (combined.Length < NonceSize + TagSize)
            throw new CryptographicException("Invalid encrypted data.");

        var nonce = new byte[NonceSize];
        var ciphertextLength = combined.Length - NonceSize - TagSize;
        var ciphertext = new byte[ciphertextLength];
        var tag = new byte[TagSize];

        Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(combined, NonceSize, ciphertext, 0, ciphertextLength);
        Buffer.BlockCopy(combined, NonceSize + ciphertextLength, tag, 0, TagSize);

        var plaintext = new byte[ciphertextLength];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    /// <summary>
    /// Encrypts a string and returns a base64-encoded result.
    /// </summary>
    public static string EncryptString(string plaintext, byte[] key)
    {
        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var encrypted = Encrypt(plaintextBytes, key);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Decrypts a base64-encoded string.
    /// </summary>
    public static string DecryptString(string encrypted, byte[] key)
    {
        var combined = Convert.FromBase64String(encrypted);
        var decrypted = Decrypt(combined, key);
        return System.Text.Encoding.UTF8.GetString(decrypted);
    }
}
