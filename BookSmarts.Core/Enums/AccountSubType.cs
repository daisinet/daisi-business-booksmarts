namespace BookSmarts.Core.Enums;

public enum AccountSubType
{
    // Asset
    Cash = 100,
    Bank = 101,
    AccountsReceivable = 102,
    Inventory = 103,
    PrepaidExpenses = 104,
    OtherCurrentAsset = 105,
    InterCompanyReceivable = 106,
    FixedAsset = 110,
    AccumulatedDepreciation = 111,
    OtherAsset = 119,

    // Liability
    AccountsPayable = 200,
    CreditCard = 201,
    AccruedLiabilities = 202,
    CurrentPortionLongTermDebt = 203,
    OtherCurrentLiability = 204,
    InterCompanyPayable = 205,
    LongTermDebt = 210,
    OtherLiability = 219,

    // Equity
    OwnersEquity = 300,
    RetainedEarnings = 301,
    CommonStock = 302,
    AdditionalPaidInCapital = 303,
    OtherEquity = 319,

    // Revenue
    SalesRevenue = 400,
    ServiceRevenue = 401,
    OtherRevenue = 402,
    InterestIncome = 403,

    // Expense
    CostOfGoodsSold = 500,
    Payroll = 501,
    Rent = 502,
    Utilities = 503,
    Insurance = 504,
    Depreciation = 505,
    OfficeExpenses = 506,
    TravelExpenses = 507,
    ProfessionalFees = 508,
    InterestExpense = 509,
    TaxExpense = 510,
    OtherExpense = 519
}
