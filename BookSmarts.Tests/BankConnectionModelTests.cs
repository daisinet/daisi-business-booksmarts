using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;

namespace BookSmarts.Tests;

public class BankConnectionModelTests
{
    [Fact]
    public void BankConnection_DefaultStatus_IsActive()
    {
        var conn = new BankConnection();
        Assert.Equal(BankConnectionStatus.Active, conn.Status);
    }

    [Fact]
    public void BankConnection_TypeProperty_IsBankConnection()
    {
        var conn = new BankConnection();
        Assert.Equal("BankConnection", conn.Type);
    }

    [Fact]
    public void BankConnection_DefaultAccounts_IsEmptyList()
    {
        var conn = new BankConnection();
        Assert.NotNull(conn.Accounts);
        Assert.Empty(conn.Accounts);
    }

    [Fact]
    public void BankAccount_DefaultIsEnabled_IsTrue()
    {
        var acct = new BankAccount();
        Assert.True(acct.IsEnabled);
    }

    [Fact]
    public void CategorizationRule_DefaultIsActive_IsTrue()
    {
        var rule = new CategorizationRule();
        Assert.True(rule.IsActive);
    }

    [Fact]
    public void CategorizationRule_TypeProperty_IsCategorizationRule()
    {
        var rule = new CategorizationRule();
        Assert.Equal("CategorizationRule", rule.Type);
    }

    [Fact]
    public void CategorizationRule_DefaultTimesApplied_IsZero()
    {
        var rule = new CategorizationRule();
        Assert.Equal(0, rule.TimesApplied);
    }
}
