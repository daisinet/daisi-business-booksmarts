using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class PaymentService(BookSmartsCosmo cosmo, AccountingService accounting, InvoiceService invoiceService, BillService billService, EncryptionContext encryption)
{
    /// <summary>
    /// Receives a customer payment: creates JE (DR Bank / CR AR), applies allocations to invoices.
    /// </summary>
    public async Task<Payment> ReceiveCustomerPaymentAsync(Payment payment)
    {
        ValidatePayment(payment, PaymentType.CustomerPayment);

        var nextNumber = await cosmo.GetNextPaymentNumberAsync(payment.CompanyId);
        payment.PaymentNumber = $"PMT-{nextNumber:D6}";
        payment.PaymentType = PaymentType.CustomerPayment;

        // Find AR and bank accounts
        var accounts = await cosmo.GetChartOfAccountsAsync(payment.CompanyId);
        DecryptAll(accounts);

        var arAccount = accounts.FirstOrDefault(a => a.SubType == AccountSubType.AccountsReceivable)
            ?? throw new InvalidOperationException("No Accounts Receivable account found.");

        var bankAccount = accounts.FirstOrDefault(a => a.id == payment.BankAccountId)
            ?? throw new InvalidOperationException("Bank account not found.");

        // Create journal entry: DR Bank / CR AR
        var journalEntry = new JournalEntry
        {
            CompanyId = payment.CompanyId,
            EntryDate = payment.PaymentDate,
            Description = $"Payment {payment.PaymentNumber} from {payment.CustomerName}",
            SourceType = SourceType.Payment,
            Lines = new List<JournalLine>
            {
                new()
                {
                    AccountId = bankAccount.id,
                    AccountNumber = bankAccount.AccountNumber,
                    AccountName = bankAccount.Name,
                    Debit = payment.Amount,
                    Credit = 0,
                    Description = $"Payment from {payment.CustomerName}"
                },
                new()
                {
                    AccountId = arAccount.id,
                    AccountNumber = arAccount.AccountNumber,
                    AccountName = arAccount.Name,
                    Debit = 0,
                    Credit = payment.Amount,
                    Description = $"Payment from {payment.CustomerName}"
                }
            }
        };

        var je = await accounting.CreateJournalEntryAsync(journalEntry);
        await accounting.PostJournalEntryAsync(je.id, payment.CompanyId);

        payment.JournalEntryId = je.id;
        payment.Status = PaymentStatus.Completed;

        Encrypt(payment);
        var created = Decrypt(await cosmo.CreatePaymentAsync(payment));

        // Apply allocations to invoices
        foreach (var alloc in created.Allocations)
        {
            if (!string.IsNullOrEmpty(alloc.InvoiceId))
                await invoiceService.ApplyPaymentToInvoiceAsync(alloc.InvoiceId, payment.CompanyId, alloc.Amount);
        }

        return created;
    }

    /// <summary>
    /// Makes a vendor payment: creates JE (DR AP / CR Bank), applies allocations to bills.
    /// </summary>
    public async Task<Payment> MakeVendorPaymentAsync(Payment payment)
    {
        ValidatePayment(payment, PaymentType.VendorPayment);

        var nextNumber = await cosmo.GetNextPaymentNumberAsync(payment.CompanyId);
        payment.PaymentNumber = $"PMT-{nextNumber:D6}";
        payment.PaymentType = PaymentType.VendorPayment;

        var accounts = await cosmo.GetChartOfAccountsAsync(payment.CompanyId);
        DecryptAll(accounts);

        var apAccount = accounts.FirstOrDefault(a => a.SubType == AccountSubType.AccountsPayable)
            ?? throw new InvalidOperationException("No Accounts Payable account found.");

        var bankAccount = accounts.FirstOrDefault(a => a.id == payment.BankAccountId)
            ?? throw new InvalidOperationException("Bank account not found.");

        // Create journal entry: DR AP / CR Bank
        var journalEntry = new JournalEntry
        {
            CompanyId = payment.CompanyId,
            EntryDate = payment.PaymentDate,
            Description = $"Payment {payment.PaymentNumber} to {payment.VendorName}",
            SourceType = SourceType.Payment,
            Lines = new List<JournalLine>
            {
                new()
                {
                    AccountId = apAccount.id,
                    AccountNumber = apAccount.AccountNumber,
                    AccountName = apAccount.Name,
                    Debit = payment.Amount,
                    Credit = 0,
                    Description = $"Payment to {payment.VendorName}"
                },
                new()
                {
                    AccountId = bankAccount.id,
                    AccountNumber = bankAccount.AccountNumber,
                    AccountName = bankAccount.Name,
                    Debit = 0,
                    Credit = payment.Amount,
                    Description = $"Payment to {payment.VendorName}"
                }
            }
        };

        var je = await accounting.CreateJournalEntryAsync(journalEntry);
        await accounting.PostJournalEntryAsync(je.id, payment.CompanyId);

        payment.JournalEntryId = je.id;
        payment.Status = PaymentStatus.Completed;

        Encrypt(payment);
        var created = Decrypt(await cosmo.CreatePaymentAsync(payment));

        // Apply allocations to bills
        foreach (var alloc in created.Allocations)
        {
            if (!string.IsNullOrEmpty(alloc.BillId))
                await billService.ApplyPaymentToBillAsync(alloc.BillId, payment.CompanyId, alloc.Amount);
        }

        return created;
    }

    /// <summary>
    /// Voids a payment: voids the JE and reverses allocations on invoices/bills.
    /// </summary>
    public async Task<Payment> VoidPaymentAsync(string id, string companyId)
    {
        var payment = Decrypt(await cosmo.GetPaymentAsync(id, companyId)
            ?? throw new InvalidOperationException("Payment not found."));

        if (payment.Status == PaymentStatus.Voided)
            throw new InvalidOperationException("Payment is already voided.");

        // Void the journal entry
        if (!string.IsNullOrEmpty(payment.JournalEntryId))
            await accounting.VoidJournalEntryAsync(payment.JournalEntryId, companyId);

        // Reverse allocations
        foreach (var alloc in payment.Allocations)
        {
            if (!string.IsNullOrEmpty(alloc.InvoiceId))
                await invoiceService.ReversePaymentFromInvoiceAsync(alloc.InvoiceId, companyId, alloc.Amount);
            if (!string.IsNullOrEmpty(alloc.BillId))
                await billService.ReversePaymentFromBillAsync(alloc.BillId, companyId, alloc.Amount);
        }

        payment.Status = PaymentStatus.Voided;
        Encrypt(payment);
        return Decrypt(await cosmo.UpdatePaymentAsync(payment));
    }

    public async Task<Payment?> GetPaymentAsync(string id, string companyId)
    {
        var payment = await cosmo.GetPaymentAsync(id, companyId);
        return payment != null ? Decrypt(payment) : null;
    }

    public async Task<List<Payment>> GetPaymentsAsync(string companyId, PaymentType? type = null, PaymentStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var payments = await cosmo.GetPaymentsAsync(companyId, type, status, fromDate, toDate);
        return DecryptAll(payments);
    }

    public static void ValidatePayment(Payment payment, PaymentType expectedType)
    {
        if (string.IsNullOrWhiteSpace(payment.CompanyId))
            throw new InvalidOperationException("Company ID is required.");
        if (payment.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(payment.BankAccountId))
            throw new InvalidOperationException("Bank account is required.");

        if (expectedType == PaymentType.CustomerPayment && string.IsNullOrWhiteSpace(payment.CustomerId))
            throw new InvalidOperationException("Customer is required for customer payments.");
        if (expectedType == PaymentType.VendorPayment && string.IsNullOrWhiteSpace(payment.VendorId))
            throw new InvalidOperationException("Vendor is required for vendor payments.");

        var allocatedTotal = payment.Allocations.Sum(a => a.Amount);
        if (allocatedTotal > payment.Amount)
            throw new InvalidOperationException("Total allocated amount cannot exceed payment amount.");
        if (payment.Allocations.Any(a => a.Amount <= 0))
            throw new InvalidOperationException("Each allocation amount must be greater than zero.");
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
