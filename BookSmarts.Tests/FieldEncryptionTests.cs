using System.Security.Cryptography;
using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class FieldEncryptionTests
{
    private static byte[] MakeAdk()
    {
        var adk = new byte[32];
        RandomNumberGenerator.Fill(adk);
        return adk;
    }

    [Fact]
    public void Organization_EncryptDecrypt_Roundtrip()
    {
        var adk = MakeAdk();
        var org = new Organization { id = "org-1", AccountId = "acc-1", Name = "Acme Corp" };

        FieldEncryption.EncryptFields(org, adk);
        Assert.NotNull(org.EncryptedPayload);
        Assert.Null(org.Name);

        FieldEncryption.DecryptFields(org, adk);
        Assert.Null(org.EncryptedPayload);
        Assert.Equal("Acme Corp", org.Name);
    }

    [Fact]
    public void Company_EncryptDecrypt_PreservesAllFields()
    {
        var adk = MakeAdk();
        var company = new Company
        {
            id = "co-1", AccountId = "acc-1", OrganizationId = "org-1",
            Name = "Test Co", TaxId = "12-3456789", Address = "123 Main St",
            Phone = "555-0100", Email = "test@test.com",
            Currency = "USD", FiscalYearStartMonth = 1
        };

        FieldEncryption.EncryptFields(company, adk);
        Assert.NotNull(company.EncryptedPayload);
        Assert.Null(company.Name);
        Assert.Null(company.TaxId);
        Assert.Null(company.Phone);
        Assert.Equal("USD", company.Currency); // Not encrypted

        FieldEncryption.DecryptFields(company, adk);
        Assert.Equal("Test Co", company.Name);
        Assert.Equal("12-3456789", company.TaxId);
        Assert.Equal("123 Main St", company.Address);
        Assert.Equal("555-0100", company.Phone);
        Assert.Equal("test@test.com", company.Email);
    }

    [Fact]
    public void JournalEntry_EncryptDecrypt_WithLines()
    {
        var adk = MakeAdk();
        var entry = new JournalEntry
        {
            id = "je-1", CompanyId = "co-1", EntryNumber = "JE-000001",
            Description = "Office supplies",
            Memo = "Q1 purchase",
            Lines = new List<JournalLine>
            {
                new() { AccountId = "coa-1", AccountNumber = "6400", AccountName = "Office Supplies", Debit = 125.50m, Credit = 0 },
                new() { AccountId = "coa-2", AccountNumber = "1000", AccountName = "Cash", Debit = 0, Credit = 125.50m }
            }
        };

        FieldEncryption.EncryptFields(entry, adk);
        Assert.NotNull(entry.EncryptedPayload);
        Assert.Null(entry.Description);
        Assert.Null(entry.Lines); // Lines encrypted

        FieldEncryption.DecryptFields(entry, adk);
        Assert.Equal("Office supplies", entry.Description);
        Assert.Equal("Q1 purchase", entry.Memo);
        Assert.Equal(2, entry.Lines.Count);
        Assert.Equal(125.50m, entry.Lines[0].Debit);
        Assert.Equal("Office Supplies", entry.Lines[0].AccountName);
    }

    [Fact]
    public void BankTransaction_DecimalPrecision_Preserved()
    {
        var adk = MakeAdk();
        var txn = new BankTransaction
        {
            id = "bt-1", CompanyId = "co-1", Name = "ACME STORE",
            Amount = 99.99m, MerchantName = "Acme Store",
            PlaidCategories = new List<string> { "FOOD_AND_DRINK", "FOOD_AND_DRINK_COFFEE" }
        };

        FieldEncryption.EncryptFields(txn, adk);
        Assert.Equal(0m, txn.Amount); // Zeroed
        Assert.Null(txn.Name);

        FieldEncryption.DecryptFields(txn, adk);
        Assert.Equal(99.99m, txn.Amount);
        Assert.Equal("ACME STORE", txn.Name);
        Assert.Equal("Acme Store", txn.MerchantName);
        Assert.Equal(2, txn.PlaidCategories.Count);
    }

    [Fact]
    public void BankConnection_WithAccounts_Roundtrip()
    {
        var adk = MakeAdk();
        var conn = new BankConnection
        {
            id = "bc-1", CompanyId = "co-1",
            InstitutionName = "Chase Bank",
            EncryptedAccessToken = "access-sandbox-abc123",
            Accounts = new List<BankAccount>
            {
                new() { PlaidAccountId = "pa-1", Name = "Checking", Mask = "1234", CurrentBalance = 5000.00m }
            }
        };

        FieldEncryption.EncryptFields(conn, adk);
        Assert.NotNull(conn.EncryptedPayload);
        Assert.Null(conn.InstitutionName);
        Assert.Null(conn.Accounts);

        FieldEncryption.DecryptFields(conn, adk);
        Assert.Equal("Chase Bank", conn.InstitutionName);
        Assert.Equal("access-sandbox-abc123", conn.EncryptedAccessToken);
        Assert.Single(conn.Accounts);
        Assert.Equal("Checking", conn.Accounts[0].Name);
        Assert.Equal(5000.00m, conn.Accounts[0].CurrentBalance);
    }

    [Fact]
    public void FiscalYear_WithPeriods_Roundtrip()
    {
        var adk = MakeAdk();
        var fy = new FiscalYear
        {
            id = "fy-1", CompanyId = "co-1",
            Name = "FY 2025",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            Periods = new List<FiscalPeriod>
            {
                new() { PeriodId = "fp-1", PeriodNumber = 1, Name = "January 2025" }
            }
        };

        FieldEncryption.EncryptFields(fy, adk);
        Assert.Null(fy.Name);
        Assert.Null(fy.Periods);

        FieldEncryption.DecryptFields(fy, adk);
        Assert.Equal("FY 2025", fy.Name);
        Assert.Single(fy.Periods);
        Assert.Equal("January 2025", fy.Periods[0].Name);
    }

    [Fact]
    public void CategorizationRule_Roundtrip()
    {
        var adk = MakeAdk();
        var rule = new CategorizationRule
        {
            id = "cr-1", CompanyId = "co-1",
            MerchantNameContains = "STARBUCKS",
            PlaidCategory = "FOOD_AND_DRINK",
            TargetAccountName = "Coffee Expense"
        };

        FieldEncryption.EncryptFields(rule, adk);
        Assert.Null(rule.MerchantNameContains);
        Assert.Null(rule.TargetAccountName);

        FieldEncryption.DecryptFields(rule, adk);
        Assert.Equal("STARBUCKS", rule.MerchantNameContains);
        Assert.Equal("FOOD_AND_DRINK", rule.PlaidCategory);
        Assert.Equal("Coffee Expense", rule.TargetAccountName);
    }

    [Fact]
    public void WrongKey_ThrowsOnDecrypt()
    {
        var adk1 = MakeAdk();
        var adk2 = MakeAdk();

        var org = new Organization { id = "org-1", Name = "Acme" };
        FieldEncryption.EncryptFields(org, adk1);

        Assert.ThrowsAny<Exception>(() => FieldEncryption.DecryptFields(org, adk2));
    }

    [Fact]
    public void NullPayload_DecryptIsNoop()
    {
        var adk = MakeAdk();
        var org = new Organization { id = "org-1", Name = "Acme" };
        // EncryptedPayload is null — decrypt should be no-op
        FieldEncryption.DecryptFields(org, adk);
        Assert.Equal("Acme", org.Name);
    }

    [Fact]
    public void ChartOfAccountEntry_Roundtrip()
    {
        var adk = MakeAdk();
        var entry = new ChartOfAccountEntry
        {
            id = "coa-1", CompanyId = "co-1",
            AccountNumber = "1000",
            Name = "Cash",
            Description = "Primary cash account",
            Category = AccountCategory.Asset
        };

        FieldEncryption.EncryptFields(entry, adk);
        Assert.Null(entry.Name);
        Assert.Null(entry.Description);
        Assert.Equal("1000", entry.AccountNumber); // Not encrypted

        FieldEncryption.DecryptFields(entry, adk);
        Assert.Equal("Cash", entry.Name);
        Assert.Equal("Primary cash account", entry.Description);
    }

    [Fact]
    public void Division_Roundtrip()
    {
        var adk = MakeAdk();
        var div = new Division { id = "div-1", AccountId = "acc-1", Name = "West Coast" };

        FieldEncryption.EncryptFields(div, adk);
        Assert.Null(div.Name);

        FieldEncryption.DecryptFields(div, adk);
        Assert.Equal("West Coast", div.Name);
    }
}
