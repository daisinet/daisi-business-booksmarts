using System.Security.Cryptography;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class TokenEncryptionTests
{
    [Fact]
    public void EncryptDecrypt_Roundtrip_ReturnsOriginal()
    {
        var key = TokenEncryption.GenerateKey();
        var plaintext = "access-sandbox-abc123-test-token";

        var encrypted = TokenEncryption.Encrypt(plaintext, key);
        var decrypted = TokenEncryption.Decrypt(encrypted, key);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_DifferentKeys_ProduceDifferentCiphertext()
    {
        var key1 = TokenEncryption.GenerateKey();
        var key2 = TokenEncryption.GenerateKey();
        var plaintext = "access-sandbox-abc123";

        var encrypted1 = TokenEncryption.Encrypt(plaintext, key1);
        var encrypted2 = TokenEncryption.Encrypt(plaintext, key2);

        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Decrypt_TamperedData_Throws()
    {
        var key = TokenEncryption.GenerateKey();
        var encrypted = TokenEncryption.Encrypt("test-token", key);

        // Tamper with the ciphertext
        var bytes = Convert.FromBase64String(encrypted);
        bytes[15] ^= 0xFF; // Flip a byte in the ciphertext
        var tampered = Convert.ToBase64String(bytes);

        Assert.ThrowsAny<CryptographicException>(() => TokenEncryption.Decrypt(tampered, key));
    }

    [Fact]
    public void Decrypt_WrongKey_Throws()
    {
        var key1 = TokenEncryption.GenerateKey();
        var key2 = TokenEncryption.GenerateKey();
        var encrypted = TokenEncryption.Encrypt("test-token", key1);

        Assert.ThrowsAny<CryptographicException>(() => TokenEncryption.Decrypt(encrypted, key2));
    }

    [Fact]
    public void EncryptDecrypt_EmptyString_Works()
    {
        var key = TokenEncryption.GenerateKey();

        var encrypted = TokenEncryption.Encrypt("", key);
        var decrypted = TokenEncryption.Decrypt(encrypted, key);

        Assert.Equal("", decrypted);
    }

    [Fact]
    public void GenerateKey_ProducesValidBase64Key()
    {
        var key = TokenEncryption.GenerateKey();

        var bytes = Convert.FromBase64String(key);
        Assert.Equal(32, bytes.Length); // 256-bit key
    }

    [Fact]
    public void GenerateKey_ProducesUniqueKeys()
    {
        var key1 = TokenEncryption.GenerateKey();
        var key2 = TokenEncryption.GenerateKey();

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void Encrypt_SamePlaintext_DifferentNonce_DifferentOutput()
    {
        var key = TokenEncryption.GenerateKey();
        var plaintext = "same-plaintext";

        var encrypted1 = TokenEncryption.Encrypt(plaintext, key);
        var encrypted2 = TokenEncryption.Encrypt(plaintext, key);

        // Should differ due to random nonce
        Assert.NotEqual(encrypted1, encrypted2);

        // But both should decrypt to the same value
        Assert.Equal(plaintext, TokenEncryption.Decrypt(encrypted1, key));
        Assert.Equal(plaintext, TokenEncryption.Decrypt(encrypted2, key));
    }
}
