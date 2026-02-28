using System.Security.Cryptography;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class KeyDerivationTests
{
    [Fact]
    public void GenerateAdk_Returns32Bytes()
    {
        var adk = KeyDerivation.GenerateAdk();
        Assert.Equal(32, adk.Length);
    }

    [Fact]
    public void GenerateAdk_ProducesUniqueKeys()
    {
        var adk1 = KeyDerivation.GenerateAdk();
        var adk2 = KeyDerivation.GenerateAdk();
        Assert.NotEqual(adk1, adk2);
    }

    [Fact]
    public void DeriveKek_Deterministic_SameInputsSameOutput()
    {
        var salt = KeyDerivation.GenerateSalt();
        var kek1 = KeyDerivation.DeriveKek("123456", salt, 1000);
        var kek2 = KeyDerivation.DeriveKek("123456", salt, 1000);
        Assert.Equal(kek1, kek2);
    }

    [Fact]
    public void DeriveKek_DifferentPins_DifferentOutput()
    {
        var salt = KeyDerivation.GenerateSalt();
        var kek1 = KeyDerivation.DeriveKek("123456", salt, 1000);
        var kek2 = KeyDerivation.DeriveKek("654321", salt, 1000);
        Assert.NotEqual(kek1, kek2);
    }

    [Fact]
    public void WrapUnwrapAdk_Roundtrip()
    {
        var adk = KeyDerivation.GenerateAdk();
        var salt = KeyDerivation.GenerateSalt();
        var kek = KeyDerivation.DeriveKek("123456", salt, 1000);

        var wrapped = KeyDerivation.WrapAdk(adk, kek);
        var unwrapped = KeyDerivation.UnwrapAdk(wrapped, kek);

        Assert.Equal(adk, unwrapped);
    }

    [Fact]
    public void UnwrapAdk_WrongPin_Throws()
    {
        var adk = KeyDerivation.GenerateAdk();
        var salt = KeyDerivation.GenerateSalt();
        var kek = KeyDerivation.DeriveKek("123456", salt, 1000);
        var wrongKek = KeyDerivation.DeriveKek("999999", salt, 1000);

        var wrapped = KeyDerivation.WrapAdk(adk, kek);

        Assert.ThrowsAny<CryptographicException>(() => KeyDerivation.UnwrapAdk(wrapped, wrongKek));
    }

    [Fact]
    public void GenerateMnemonic_Returns12Words()
    {
        var mnemonic = KeyDerivation.GenerateMnemonic();
        var words = mnemonic.Split(' ');
        Assert.Equal(12, words.Length);
    }

    [Fact]
    public void GenerateMnemonic_AllWordsInWordList()
    {
        var mnemonic = KeyDerivation.GenerateMnemonic();
        var words = mnemonic.Split(' ');
        var wordSet = new HashSet<string>(Core.Encryption.Bip39Words.Words);

        foreach (var word in words)
        {
            Assert.Contains(word, wordSet);
        }
    }

    [Fact]
    public void GenerateMnemonic_ProducesUniqueResults()
    {
        var m1 = KeyDerivation.GenerateMnemonic();
        var m2 = KeyDerivation.GenerateMnemonic();
        Assert.NotEqual(m1, m2);
    }

    [Fact]
    public void MnemonicCanWrapAndUnwrapAdk()
    {
        var adk = KeyDerivation.GenerateAdk();
        var mnemonic = KeyDerivation.GenerateMnemonic();
        var salt = KeyDerivation.GenerateSalt();
        var kek = KeyDerivation.DeriveKek(mnemonic, salt, 1000);

        var wrapped = KeyDerivation.WrapAdk(adk, kek);
        var unwrapped = KeyDerivation.UnwrapAdk(wrapped, kek);

        Assert.Equal(adk, unwrapped);
    }
}
