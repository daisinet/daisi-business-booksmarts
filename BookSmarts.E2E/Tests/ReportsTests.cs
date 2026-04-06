namespace BookSmarts.E2E.Tests;

/// <summary>
/// Tests for all financial reports: Balance Sheet, Income Statement, Cash Flow,
/// Trial Balance, Budget vs Actual, AR/AP Aging, Custom Reports, and Consolidated reports.
/// </summary>
public class ReportsTests : AuthenticatedTestBase
{
    [Fact]
    public async Task BalanceSheet_NavigateAndLoad()
    {
        await NavigateTo("/reports/balance-sheet");
        await WaitForBlazor();
        await AssertHeading("Balance Sheet");
        await TakeScreenshot("balance_sheet");
    }

    [Fact]
    public async Task IncomeStatement_NavigateAndLoad()
    {
        await NavigateTo("/reports/income-statement");
        await WaitForBlazor();
        await AssertHeading("Income Statement");
        await TakeScreenshot("income_statement");
    }

    [Fact]
    public async Task CashFlow_NavigateAndLoad()
    {
        await NavigateTo("/reports/cash-flow");
        await WaitForBlazor();
        await AssertHeading("Cash Flow");
        await TakeScreenshot("cash_flow");
    }

    [Fact]
    public async Task TrialBalance_NavigateAndLoad()
    {
        await NavigateTo("/trial-balance");
        await WaitForBlazor();
        await AssertHeading("Trial Balance");
        await TakeScreenshot("trial_balance");
    }

    [Fact]
    public async Task BudgetVsActual_NavigateAndLoad()
    {
        await NavigateTo("/reports/budget-vs-actual");
        await WaitForBlazor();
        await AssertHeading("Budget vs Actual");
        await TakeScreenshot("budget_vs_actual");
    }

    [Fact]
    public async Task ARAgingReport_NavigateAndLoad()
    {
        await NavigateTo("/reports/ar-aging");
        await WaitForBlazor();
        await TakeScreenshot("ar_aging");
    }

    [Fact]
    public async Task APAgingReport_NavigateAndLoad()
    {
        await NavigateTo("/reports/ap-aging");
        await WaitForBlazor();
        await TakeScreenshot("ap_aging");
    }

    [Fact]
    public async Task CustomReports_NavigateAndLoad()
    {
        await NavigateTo("/reports/custom");
        await WaitForBlazor();
        await TakeScreenshot("custom_reports");
    }

    [Fact]
    public async Task ConsolidatedBalanceSheet_NavigateAndLoad()
    {
        await NavigateTo("/reports/consolidated-balance-sheet");
        await WaitForBlazor();
        await TakeScreenshot("consolidated_balance_sheet");
    }

    [Fact]
    public async Task ConsolidatedIncomeStatement_NavigateAndLoad()
    {
        await NavigateTo("/reports/consolidated-income-statement");
        await WaitForBlazor();
        await TakeScreenshot("consolidated_income_statement");
    }
}
