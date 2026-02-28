using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class JournalEntryValidationTests
{
    [Fact]
    public void ValidateJournalEntry_BalancedEntry_Succeeds()
    {
        var entry = MakeBalancedEntry();
        var ex = Record.Exception(() => AccountingService.ValidateJournalEntry(entry));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateJournalEntry_UnbalancedEntry_ThrowsWithMessage()
    {
        var entry = new JournalEntry
        {
            CompanyId = "co-test",
            Lines = new()
            {
                new JournalLine { AccountId = "a1", Debit = 100, Credit = 0 },
                new JournalLine { AccountId = "a2", Debit = 0, Credit = 50 }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => AccountingService.ValidateJournalEntry(entry));
        Assert.Contains("not balanced", ex.Message);
    }

    [Fact]
    public void ValidateJournalEntry_LessThanTwoLines_Throws()
    {
        var entry = new JournalEntry
        {
            CompanyId = "co-test",
            Lines = new()
            {
                new JournalLine { AccountId = "a1", Debit = 100, Credit = 0 }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => AccountingService.ValidateJournalEntry(entry));
        Assert.Contains("at least two lines", ex.Message);
    }

    [Fact]
    public void ValidateJournalEntry_NegativeAmounts_Throws()
    {
        var entry = new JournalEntry
        {
            CompanyId = "co-test",
            Lines = new()
            {
                new JournalLine { AccountId = "a1", Debit = -100, Credit = 0 },
                new JournalLine { AccountId = "a2", Debit = 0, Credit = -100 }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => AccountingService.ValidateJournalEntry(entry));
        Assert.Contains("non-negative", ex.Message);
    }

    [Fact]
    public void ValidateJournalEntry_BothDebitAndCredit_Throws()
    {
        var entry = new JournalEntry
        {
            CompanyId = "co-test",
            Lines = new()
            {
                new JournalLine { AccountId = "a1", Debit = 100, Credit = 50 },
                new JournalLine { AccountId = "a2", Debit = 0, Credit = 50 }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => AccountingService.ValidateJournalEntry(entry));
        Assert.Contains("cannot have both", ex.Message);
    }

    [Fact]
    public void ValidateJournalEntry_ZeroLine_Throws()
    {
        var entry = new JournalEntry
        {
            CompanyId = "co-test",
            Lines = new()
            {
                new JournalLine { AccountId = "a1", Debit = 100, Credit = 0 },
                new JournalLine { AccountId = "a2", Debit = 0, Credit = 0 }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => AccountingService.ValidateJournalEntry(entry));
        Assert.Contains("debit or credit amount", ex.Message);
    }

    [Fact]
    public void ValidateJournalEntry_MissingCompanyId_Throws()
    {
        var entry = new JournalEntry
        {
            CompanyId = "",
            Lines = new()
            {
                new JournalLine { AccountId = "a1", Debit = 100, Credit = 0 },
                new JournalLine { AccountId = "a2", Debit = 0, Credit = 100 }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => AccountingService.ValidateJournalEntry(entry));
        Assert.Contains("Company ID", ex.Message);
    }

    [Fact]
    public void ValidateJournalEntry_MissingAccountId_Throws()
    {
        var entry = new JournalEntry
        {
            CompanyId = "co-test",
            Lines = new()
            {
                new JournalLine { AccountId = "a1", Debit = 100, Credit = 0 },
                new JournalLine { AccountId = "", Debit = 0, Credit = 100 }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => AccountingService.ValidateJournalEntry(entry));
        Assert.Contains("account specified", ex.Message);
    }

    [Fact]
    public void ValidateJournalEntry_MultiLineBalanced_Succeeds()
    {
        var entry = new JournalEntry
        {
            CompanyId = "co-test",
            Lines = new()
            {
                new JournalLine { AccountId = "a1", Debit = 500, Credit = 0 },
                new JournalLine { AccountId = "a2", Debit = 0, Credit = 300 },
                new JournalLine { AccountId = "a3", Debit = 0, Credit = 200 }
            }
        };

        var ex = Record.Exception(() => AccountingService.ValidateJournalEntry(entry));
        Assert.Null(ex);
    }

    [Fact]
    public void JournalEntry_IsBalanced_ReturnsCorrectly()
    {
        var balanced = MakeBalancedEntry();
        Assert.True(balanced.IsBalanced);

        var unbalanced = new JournalEntry
        {
            Lines = new()
            {
                new JournalLine { Debit = 100 },
                new JournalLine { Credit = 50 }
            }
        };
        Assert.False(unbalanced.IsBalanced);
    }

    [Fact]
    public void JournalEntry_Totals_CalculatedCorrectly()
    {
        var entry = new JournalEntry
        {
            Lines = new()
            {
                new JournalLine { Debit = 100, Credit = 0 },
                new JournalLine { Debit = 200, Credit = 0 },
                new JournalLine { Debit = 0, Credit = 300 }
            }
        };

        Assert.Equal(300, entry.TotalDebit);
        Assert.Equal(300, entry.TotalCredit);
    }

    private static JournalEntry MakeBalancedEntry() => new()
    {
        CompanyId = "co-test",
        Lines = new()
        {
            new JournalLine { AccountId = "a1", AccountNumber = "1000", AccountName = "Cash", Debit = 1000, Credit = 0 },
            new JournalLine { AccountId = "a2", AccountNumber = "4000", AccountName = "Revenue", Debit = 0, Credit = 1000 }
        }
    };
}
