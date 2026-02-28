using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class BillService(BookSmartsCosmo cosmo, AccountingService accounting, ChartOfAccountsService coaService, EncryptionContext encryption, AuditService audit)
{
    /// <summary>
    /// Creates a new bill in Draft status with auto-generated number and computed DueDate.
    /// </summary>
    public async Task<Bill> CreateBillAsync(Bill bill)
    {
        ValidateBill(bill);

        var nextNumber = await cosmo.GetNextBillNumberAsync(bill.CompanyId);
        bill.BillNumber = $"BILL-{nextNumber:D6}";
        bill.Status = BillStatus.Draft;

        if (bill.DueDate == default)
            bill.DueDate = ComputeDueDate(bill.BillDate, bill.PaymentTerms);

        for (int i = 0; i < bill.Lines.Count; i++)
            bill.Lines[i].LineNumber = i + 1;

        Encrypt(bill);
        var created = Decrypt(await cosmo.CreateBillAsync(bill));
        await audit.LogAsync(created.CompanyId, "", "Created", nameof(Bill), created.id,
            created.BillNumber, $"Bill created from {created.VendorName} ({created.Total:C})");
        return created;
    }

    /// <summary>
    /// Transitions a bill from Draft to Received and creates the accrual journal entry.
    /// DR Expense per line / CR Accounts Payable.
    /// </summary>
    public async Task<Bill> ReceiveBillAsync(string id, string companyId)
    {
        var bill = Decrypt(await cosmo.GetBillAsync(id, companyId)
            ?? throw new InvalidOperationException("Bill not found."));

        if (bill.Status != BillStatus.Draft)
            throw new InvalidOperationException("Only draft bills can be received.");

        var accounts = await coaService.GetChartOfAccountsAsync(companyId);
        var apAccount = accounts.FirstOrDefault(a => a.SubType == AccountSubType.AccountsPayable)
            ?? throw new InvalidOperationException("No Accounts Payable account found in chart of accounts.");

        var jeLines = new List<JournalLine>();

        foreach (var line in bill.Lines)
        {
            jeLines.Add(new JournalLine
            {
                AccountId = line.AccountId,
                AccountNumber = line.AccountNumber,
                AccountName = line.AccountName,
                Debit = line.Amount,
                Credit = 0,
                Description = line.Description
            });
        }

        jeLines.Add(new JournalLine
        {
            AccountId = apAccount.id,
            AccountNumber = apAccount.AccountNumber,
            AccountName = apAccount.Name,
            Debit = 0,
            Credit = bill.Total,
            Description = $"Bill {bill.BillNumber} - {bill.VendorName}"
        });

        var journalEntry = new JournalEntry
        {
            CompanyId = companyId,
            EntryDate = bill.BillDate,
            Description = $"Bill {bill.BillNumber} from {bill.VendorName}",
            SourceType = SourceType.Bill,
            SourceId = bill.id,
            Lines = jeLines
        };

        var je = await accounting.CreateJournalEntryAsync(journalEntry);
        await accounting.PostJournalEntryAsync(je.id, companyId);

        bill.Status = BillStatus.Received;
        bill.JournalEntryId = je.id;
        Encrypt(bill);
        var received = Decrypt(await cosmo.UpdateBillAsync(bill));
        await audit.LogAsync(companyId, "", "Received", nameof(Bill), id,
            received.BillNumber, $"Bill received from {received.VendorName} ({received.Total:C})");
        return received;
    }

    /// <summary>
    /// Updates a bill that is still in Draft status.
    /// </summary>
    public async Task<Bill> UpdateDraftBillAsync(Bill bill)
    {
        if (bill.Status != BillStatus.Draft)
            throw new InvalidOperationException("Only draft bills can be edited.");

        ValidateBill(bill);

        for (int i = 0; i < bill.Lines.Count; i++)
            bill.Lines[i].LineNumber = i + 1;

        Encrypt(bill);
        return Decrypt(await cosmo.UpdateBillAsync(bill));
    }

    /// <summary>
    /// Voids a bill and its associated journal entry.
    /// </summary>
    public async Task<Bill> VoidBillAsync(string id, string companyId)
    {
        var bill = Decrypt(await cosmo.GetBillAsync(id, companyId)
            ?? throw new InvalidOperationException("Bill not found."));

        if (bill.Status == BillStatus.Voided)
            throw new InvalidOperationException("Bill is already voided.");

        if (!string.IsNullOrEmpty(bill.JournalEntryId))
            await accounting.VoidJournalEntryAsync(bill.JournalEntryId, companyId);

        bill.Status = BillStatus.Voided;
        Encrypt(bill);
        return Decrypt(await cosmo.UpdateBillAsync(bill));
    }

    public async Task<Bill?> GetBillAsync(string id, string companyId)
    {
        var bill = await cosmo.GetBillAsync(id, companyId);
        return bill != null ? Decrypt(bill) : null;
    }

    public async Task<List<Bill>> GetBillsAsync(string companyId, BillStatus? status = null, string? vendorId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var bills = await cosmo.GetBillsAsync(companyId, status, vendorId, fromDate, toDate);
        return DecryptAll(bills);
    }

    public async Task<List<Bill>> GetOpenBillsAsync(string companyId, string? vendorId = null)
    {
        var bills = await cosmo.GetOpenBillsAsync(companyId, vendorId);
        return DecryptAll(bills);
    }

    /// <summary>
    /// Applies a payment amount to a bill and recalculates status.
    /// </summary>
    public async Task ApplyPaymentToBillAsync(string billId, string companyId, decimal paymentAmount)
    {
        var bill = Decrypt(await cosmo.GetBillAsync(billId, companyId)
            ?? throw new InvalidOperationException("Bill not found."));

        var newAmountPaid = bill.AmountPaid + paymentAmount;
        var newBalanceDue = bill.Total - newAmountPaid;

        BillStatus newStatus;
        if (newBalanceDue <= 0)
            newStatus = BillStatus.Paid;
        else if (newAmountPaid > 0)
            newStatus = BillStatus.PartiallyPaid;
        else
            newStatus = bill.Status;

        await cosmo.PatchBillPaymentAsync(billId, companyId, newAmountPaid, newStatus);
    }

    /// <summary>
    /// Reverses a payment amount from a bill and recalculates status.
    /// </summary>
    public async Task ReversePaymentFromBillAsync(string billId, string companyId, decimal paymentAmount)
    {
        var bill = Decrypt(await cosmo.GetBillAsync(billId, companyId)
            ?? throw new InvalidOperationException("Bill not found."));

        var newAmountPaid = Math.Max(0, bill.AmountPaid - paymentAmount);
        var newBalanceDue = bill.Total - newAmountPaid;

        BillStatus newStatus;
        if (newAmountPaid == 0)
            newStatus = BillStatus.Received;
        else if (newBalanceDue > 0)
            newStatus = BillStatus.PartiallyPaid;
        else
            newStatus = BillStatus.Paid;

        await cosmo.PatchBillPaymentAsync(billId, companyId, newAmountPaid, newStatus);
    }

    /// <summary>
    /// Finds bills past their DueDate and updates status to Overdue.
    /// </summary>
    public async Task CheckOverdueBillsAsync(string companyId)
    {
        var openBills = await cosmo.GetOpenBillsAsync(companyId);
        DecryptAll(openBills);

        var today = DateTime.UtcNow.Date;
        foreach (var bill in openBills)
        {
            if (bill.DueDate < today && bill.Status is BillStatus.Received or BillStatus.PartiallyPaid)
            {
                await cosmo.PatchBillPaymentAsync(bill.id, companyId, bill.AmountPaid, BillStatus.Overdue);
            }
        }
    }

    public static DateTime ComputeDueDate(DateTime billDate, PaymentTerms terms)
    {
        return terms == PaymentTerms.DueOnReceipt
            ? billDate
            : billDate.AddDays((int)terms);
    }

    public static void ValidateBill(Bill bill)
    {
        if (string.IsNullOrWhiteSpace(bill.CompanyId))
            throw new InvalidOperationException("Company ID is required.");
        if (string.IsNullOrWhiteSpace(bill.VendorId))
            throw new InvalidOperationException("Vendor is required.");
        if (bill.Lines == null || bill.Lines.Count == 0)
            throw new InvalidOperationException("Bill must have at least one line item.");
        if (bill.Lines.Any(l => l.Quantity <= 0))
            throw new InvalidOperationException("Line item quantity must be greater than zero.");
        if (bill.Lines.Any(l => l.UnitPrice < 0))
            throw new InvalidOperationException("Line item unit price cannot be negative.");
        if (bill.Lines.Any(l => string.IsNullOrWhiteSpace(l.AccountId)))
            throw new InvalidOperationException("All line items must have an expense account selected.");
        if (bill.Lines.Any(l => string.IsNullOrWhiteSpace(l.Description)))
            throw new InvalidOperationException("All line items must have a description.");
    }

    // ── Encryption helpers ──

    private void Encrypt<T>(T model) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.EncryptFields(model, adk);
    }

    private T Decrypt<T>(T model) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.DecryptFields(model, adk);
        return model;
    }

    private List<T> DecryptAll<T>(List<T> models) where T : class
    {
        var adk = encryption.GetAdkOrNull();
        if (adk != null)
            FieldEncryption.DecryptAll(models, adk);
        return models;
    }
}
