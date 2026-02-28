using System.Security.Cryptography;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class AesGcmHelperTests
{
    private static byte[] GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    [Fact]
    public void EncryptDecrypt_Bytes_Roundtrip()
    {
        var key = GenerateKey();
        var plaintext = "Hello, World!"u8.ToArray();

        var encrypted = AesGcmHelper.Encrypt(plaintext, key);
        var decrypted = AesGcmHelper.Decrypt(encrypted, key);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_String_Roundtrip()
    {
        var key = GenerateKey();
        var plaintext = "Sensitive financial data: $1,234.56";

        var encrypted = AesGcmHelper.EncryptString(plaintext, key);
        var decrypted = AesGcmHelper.DecryptString(encrypted, key);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WrongKey_Throws()
    {
        var key1 = GenerateKey();
        var key2 = GenerateKey();
        var encrypted = AesGcmHelper.Encrypt("test"u8.ToArray(), key1);

        Assert.ThrowsAny<CryptographicException>(() => AesGcmHelper.Decrypt(encrypted, key2));
    }

    [Fact]
    public void Decrypt_TamperedData_Throws()
    {
        var key = GenerateKey();
        var encrypted = AesGcmHelper.Encrypt("test"u8.ToArray(), key);

        encrypted[15] ^= 0xFF; // Tamper

        Assert.ThrowsAny<CryptographicException>(() => AesGcmHelper.Decrypt(encrypted, key));
    }

    [Fact]
    public void Encrypt_SamePlaintext_DifferentOutput()
    {
        var key = GenerateKey();
        var plaintext = "same data"u8.ToArray();

        var enc1 = AesGcmHelper.Encrypt(plaintext, key);
        var enc2 = AesGcmHelper.Encrypt(plaintext, key);

        Assert.NotEqual(enc1, enc2); // Different nonces
        Assert.Equal(plaintext, AesGcmHelper.Decrypt(enc1, key));
        Assert.Equal(plaintext, AesGcmHelper.Decrypt(enc2, key));
    }

    [Fact]
    public void EncryptDecrypt_EmptyData_Works()
    {
        var key = GenerateKey();
        var encrypted = AesGcmHelper.EncryptString("", key);
        var decrypted = AesGcmHelper.DecryptString(encrypted, key);
        Assert.Equal("", decrypted);
    }
}
