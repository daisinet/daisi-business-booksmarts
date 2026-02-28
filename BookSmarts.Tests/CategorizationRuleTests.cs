using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class CategorizationRuleTests
{
    [Fact]
    public void ApplyRules_MerchantNameMatch_ReturnsRule()
    {
        var txn = MakeTransaction(merchantName: "Starbucks Coffee");
        var rules = new List<CategorizationRule>
        {
            MakeRule("Starbucks", priority: 1)
        };

        var result = BankingService.ApplyCategorizationRules(txn, rules);

        Assert.NotNull(result);
        Assert.Equal("Starbucks", result.MerchantNameContains);
    }

    [Fact]
    public void ApplyRules_PlaidCategoryMatch_ReturnsRule()
    {
        var txn = MakeTransaction(categories: new List<string> { "Food and Drink", "Restaurants" });
        var rules = new List<CategorizationRule>
        {
            new()
            {
                id = "rule-1", CompanyId = "co-test", PlaidCategory = "Food and Drink",
                TargetAccountId = "acct-1", TargetAccountNumber = "5000", TargetAccountName = "Food Expense",
                Priority = 1, IsActive = true
            }
        };

        var result = BankingService.ApplyCategorizationRules(txn, rules);

        Assert.NotNull(result);
        Assert.Equal("Food and Drink", result.PlaidCategory);
    }

    [Fact]
    public void ApplyRules_NoMatch_ReturnsNull()
    {
        var txn = MakeTransaction(merchantName: "Apple Inc.");
        var rules = new List<CategorizationRule>
        {
            MakeRule("Starbucks", priority: 1)
        };

        var result = BankingService.ApplyCategorizationRules(txn, rules);

        Assert.Null(result);
    }

    [Fact]
    public void ApplyRules_PriorityOrdering_FirstMatchWins()
    {
        var txn = MakeTransaction(merchantName: "Starbucks Coffee Shop");
        var rules = new List<CategorizationRule>
        {
            MakeRule("Coffee", priority: 10, targetName: "Coffee Expense"),
            MakeRule("Starbucks", priority: 1, targetName: "Starbucks Expense")
        };

        var result = BankingService.ApplyCategorizationRules(txn, rules);

        Assert.NotNull(result);
        Assert.Equal("Starbucks Expense", result.TargetAccountName);
    }

    [Fact]
    public void ApplyRules_InactiveRule_Skipped()
    {
        var txn = MakeTransaction(merchantName: "Starbucks Coffee");
        var rules = new List<CategorizationRule>
        {
            new()
            {
                id = "rule-1", CompanyId = "co-test", MerchantNameContains = "Starbucks",
                TargetAccountId = "acct-1", TargetAccountNumber = "5000", TargetAccountName = "Food",
                Priority = 1, IsActive = false
            }
        };

        var result = BankingService.ApplyCategorizationRules(txn, rules);

        Assert.Null(result);
    }

    [Fact]
    public void ApplyRules_AlreadyCategorized_ReturnsNull()
    {
        var txn = MakeTransaction(merchantName: "Starbucks Coffee");
        txn.Status = BankTransactionStatus.Categorized;
        var rules = new List<CategorizationRule>
        {
            MakeRule("Starbucks", priority: 1)
        };

        var result = BankingService.ApplyCategorizationRules(txn, rules);

        Assert.Null(result);
    }

    [Fact]
    public void ApplyRules_CaseInsensitive_Matches()
    {
        var txn = MakeTransaction(merchantName: "STARBUCKS COFFEE");
        var rules = new List<CategorizationRule>
        {
            MakeRule("starbucks", priority: 1)
        };

        var result = BankingService.ApplyCategorizationRules(txn, rules);

        Assert.NotNull(result);
    }

    [Fact]
    public void ApplyRules_PartialMatch_Matches()
    {
        var txn = MakeTransaction(merchantName: "Starbucks Coffee #12345");
        var rules = new List<CategorizationRule>
        {
            MakeRule("Starbucks", priority: 1)
        };

        var result = BankingService.ApplyCategorizationRules(txn, rules);

        Assert.NotNull(result);
    }

    [Fact]
    public void ApplyRules_FallsBackToName_WhenMerchantNull()
    {
        var txn = MakeTransaction(merchantName: null);
        txn.Name = "STARBUCKS COFFEE";
        var rules = new List<CategorizationRule>
        {
            MakeRule("Starbucks", priority: 1)
        };

        var result = BankingService.ApplyCategorizationRules(txn, rules);

        Assert.NotNull(result);
    }

    [Fact]
    public void ApplyRules_EmptyRules_ReturnsNull()
    {
        var txn = MakeTransaction(merchantName: "Starbucks");
        var result = BankingService.ApplyCategorizationRules(txn, new List<CategorizationRule>());

        Assert.Null(result);
    }

    private static BankTransaction MakeTransaction(string? merchantName = null, List<string>? categories = null)
    {
        return new BankTransaction
        {
            id = "bt-test",
            CompanyId = "co-test",
            BankConnectionId = "bc-test",
            PlaidAccountId = "pa-test",
            PlaidTransactionId = "pt-test",
            MerchantName = merchantName,
            Name = merchantName ?? "Transaction",
            Amount = 12.50m,
            TransactionDate = DateTime.UtcNow,
            Status = BankTransactionStatus.Uncategorized,
            PlaidCategories = categories ?? new()
        };
    }

    private static CategorizationRule MakeRule(string merchantContains, int priority, string targetName = "Expense")
    {
        return new CategorizationRule
        {
            id = $"rule-{merchantContains}",
            CompanyId = "co-test",
            MerchantNameContains = merchantContains,
            TargetAccountId = "acct-1",
            TargetAccountNumber = "5000",
            TargetAccountName = targetName,
            Priority = priority,
            IsActive = true
        };
    }
}
