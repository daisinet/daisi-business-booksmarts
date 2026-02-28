using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;

namespace BookSmarts.Tests;

public class BankTransactionModelTests
{
    [Fact]
    public void BankTransaction_DefaultStatus_IsUncategorized()
    {
        var txn = new BankTransaction();
        Assert.Equal(BankTransactionStatus.Uncategorized, txn.Status);
    }

    [Fact]
    public void BankTransaction_TypeProperty_IsBankTransaction()
    {
        var txn = new BankTransaction();
        Assert.Equal("BankTransaction", txn.Type);
    }

    [Fact]
    public void BankTransaction_DefaultCollections_AreEmpty()
    {
        var txn = new BankTransaction();
        Assert.NotNull(txn.PlaidCategories);
        Assert.Empty(txn.PlaidCategories);
    }

    [Fact]
    public void BankTransaction_DefaultId_IsEmptyString()
    {
        var txn = new BankTransaction();
        Assert.Equal("", txn.id);
    }

    [Fact]
    public void BankTransaction_DefaultIsPending_IsFalse()
    {
        var txn = new BankTransaction();
        Assert.False(txn.IsPending);
    }
}
