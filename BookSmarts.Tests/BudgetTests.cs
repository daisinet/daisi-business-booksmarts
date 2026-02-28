using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class BudgetTests
{
    // ── ValidateBudget ──

    [Fact]
    public void ValidateBudget_MissingCompanyId_Throws()
    {
        var budget = new Budget { FiscalYearId = "fy-1", Name = "Test" };
        Assert.Throws<InvalidOperationException>(() => BudgetService.ValidateBudget(budget));
    }

    [Fact]
    public void ValidateBudget_MissingFiscalYearId_Throws()
    {
        var budget = new Budget { CompanyId = "c-1", Name = "Test" };
        Assert.Throws<InvalidOperationException>(() => BudgetService.ValidateBudget(budget));
    }

    [Fact]
    public void ValidateBudget_MissingName_Throws()
    {
        var budget = new Budget { CompanyId = "c-1", FiscalYearId = "fy-1", Name = "" };
        Assert.Throws<InvalidOperationException>(() => BudgetService.ValidateBudget(budget));
    }

    [Fact]
    public void ValidateBudget_NegativeAmount_Throws()
    {
        var budget = new Budget
        {
            CompanyId = "c-1",
            FiscalYearId = "fy-1",
            Name = "Budget 2026",
            LineItems = new()
            {
                new BudgetLineItem { AccountId = "a-1", PeriodId = "p-1", Amount = -100 }
            }
        };
        Assert.Throws<InvalidOperationException>(() => BudgetService.ValidateBudget(budget));
    }

    [Fact]
    public void ValidateBudget_ValidBudget_Passes()
    {
        var budget = new Budget
        {
            CompanyId = "c-1",
            FiscalYearId = "fy-1",
            Name = "Budget 2026",
            LineItems = new()
            {
                new BudgetLineItem { AccountId = "a-1", PeriodId = "p-1", Amount = 500 }
            }
        };

        var ex = Record.Exception(() => BudgetService.ValidateBudget(budget));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateBudget_MissingAccountIdOnLineItem_Throws()
    {
        var budget = new Budget
        {
            CompanyId = "c-1",
            FiscalYearId = "fy-1",
            Name = "Budget",
            LineItems = new()
            {
                new BudgetLineItem { AccountId = "", PeriodId = "p-1", Amount = 100 }
            }
        };
        Assert.Throws<InvalidOperationException>(() => BudgetService.ValidateBudget(budget));
    }

    [Fact]
    public void ValidateBudget_MissingPeriodIdOnLineItem_Throws()
    {
        var budget = new Budget
        {
            CompanyId = "c-1",
            FiscalYearId = "fy-1",
            Name = "Budget",
            LineItems = new()
            {
                new BudgetLineItem { AccountId = "a-1", PeriodId = "", Amount = 100 }
            }
        };
        Assert.Throws<InvalidOperationException>(() => BudgetService.ValidateBudget(budget));
    }

    // ── Model properties ──

    [Fact]
    public void Budget_DefaultStatus_IsDraft()
    {
        var budget = new Budget();
        Assert.Equal(BudgetStatus.Draft, budget.Status);
    }

    [Fact]
    public void Budget_EmptyLineItems_ByDefault()
    {
        var budget = new Budget();
        Assert.Empty(budget.LineItems);
    }

    [Fact]
    public void BudgetLineItem_Properties_SetCorrectly()
    {
        var item = new BudgetLineItem
        {
            AccountId = "a-1",
            AccountNumber = "4000",
            AccountName = "Sales Revenue",
            PeriodId = "p-1",
            PeriodNumber = 1,
            Amount = 1000m
        };

        Assert.Equal("a-1", item.AccountId);
        Assert.Equal("4000", item.AccountNumber);
        Assert.Equal("Sales Revenue", item.AccountName);
        Assert.Equal(1, item.PeriodNumber);
        Assert.Equal(1000m, item.Amount);
    }
}
