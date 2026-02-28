namespace BookSmarts.Core.Enums;

/// <summary>
/// Identifies how a journal entry was created, used for accrual vs cash basis filtering.
/// </summary>
public enum SourceType
{
    Manual = 0,
    BankImport = 1,
    Invoice = 2,
    Bill = 3,
    Payment = 4,
    InterCompany = 5,
    Adjustment = 6,
    Reversal = 7
}
