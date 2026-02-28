using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class InterCompanyTests
{
    // ── ValidateRequest ──

    [Fact]
    public void ValidateRequest_MissingOrganizationId_Throws()
    {
        var request = MakeValidRequest();
        request.OrganizationId = "";
        Assert.Throws<InvalidOperationException>(() => InterCompanyService.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_MissingAccountId_Throws()
    {
        var request = MakeValidRequest();
        request.AccountId = "";
        Assert.Throws<InvalidOperationException>(() => InterCompanyService.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_SameSourceAndTarget_Throws()
    {
        var request = MakeValidRequest();
        request.TargetCompanyId = request.SourceCompanyId;
        Assert.Throws<InvalidOperationException>(() => InterCompanyService.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_ZeroAmount_Throws()
    {
        var request = MakeValidRequest();
        request.Amount = 0;
        Assert.Throws<InvalidOperationException>(() => InterCompanyService.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_NegativeAmount_Throws()
    {
        var request = MakeValidRequest();
        request.Amount = -100;
        Assert.Throws<InvalidOperationException>(() => InterCompanyService.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_MissingSourceAccountId_Throws()
    {
        var request = MakeValidRequest();
        request.SourceAccountId = "";
        Assert.Throws<InvalidOperationException>(() => InterCompanyService.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_MissingTargetAccountId_Throws()
    {
        var request = MakeValidRequest();
        request.TargetAccountId = "";
        Assert.Throws<InvalidOperationException>(() => InterCompanyService.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_MissingSourceIcAccountId_Throws()
    {
        var request = MakeValidRequest();
        request.SourceIcAccountId = "";
        Assert.Throws<InvalidOperationException>(() => InterCompanyService.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_MissingTargetIcAccountId_Throws()
    {
        var request = MakeValidRequest();
        request.TargetIcAccountId = "";
        Assert.Throws<InvalidOperationException>(() => InterCompanyService.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_ValidRequest_Passes()
    {
        var request = MakeValidRequest();
        var ex = Record.Exception(() => InterCompanyService.ValidateRequest(request));
        Assert.Null(ex);
    }

    // ── Model defaults ──

    [Fact]
    public void InterCompanyTransaction_DefaultStatus_IsPosted()
    {
        var tx = new InterCompanyTransaction();
        Assert.Equal(InterCompanyStatus.Posted, tx.Status);
    }

    [Fact]
    public void InterCompanyTransaction_DefaultEliminateOnConsolidation_IsTrue()
    {
        var tx = new InterCompanyTransaction();
        Assert.True(tx.EliminateOnConsolidation);
    }

    [Fact]
    public void InterCompanyTransaction_TypeField_IsCorrect()
    {
        var tx = new InterCompanyTransaction();
        Assert.Equal("InterCompanyTransaction", tx.Type);
    }

    // ── Helper ──

    private static InterCompanyTransactionRequest MakeValidRequest() => new()
    {
        OrganizationId = "org-1",
        AccountId = "acc-1",
        SourceCompanyId = "company-a",
        TargetCompanyId = "company-b",
        SourceAccountId = "sa-1",
        TargetAccountId = "ta-1",
        SourceIcAccountId = "sic-1",
        TargetIcAccountId = "tic-1",
        Amount = 1000,
        Description = "Test IC transaction",
        TransactionDate = DateTime.Today
    };
}
