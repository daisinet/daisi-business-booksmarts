using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Data;

namespace BookSmarts.Services;

public class InvoiceService(BookSmartsCosmo cosmo, AccountingService accounting, ChartOfAccountsService coaService, EncryptionContext encryption, AuditService audit)
{
    /// <summary>
    /// Creates a new invoice in Draft status with auto-generated number and computed DueDate.
    /// </summary>
    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
    {
        ValidateInvoice(invoice);

        var nextNumber = await cosmo.GetNextInvoiceNumberAsync(invoice.CompanyId);
        invoice.InvoiceNumber = $"INV-{nextNumber:D6}";
        invoice.Status = InvoiceStatus.Draft;

        // Compute DueDate from PaymentTerms
        if (invoice.DueDate == default)
            invoice.DueDate = ComputeDueDate(invoice.InvoiceDate, invoice.PaymentTerms);

        // Number the lines
        for (int i = 0; i < invoice.Lines.Count; i++)
            invoice.Lines[i].LineNumber = i + 1;

        Encrypt(invoice);
        var created = Decrypt(await cosmo.CreateInvoiceAsync(invoice));
        await audit.LogAsync(created.CompanyId, "", "Created", nameof(Invoice), created.id,
            created.InvoiceNumber, $"Invoice created for {created.CustomerName} ({created.Total:C})");
        return created;
    }

    /// <summary>
    /// Transitions an invoice from Draft to Sent and creates the accrual journal entry.
    /// DR Accounts Receivable / CR Revenue per line.
    /// </summary>
    public async Task<Invoice> SendInvoiceAsync(string id, string companyId)
    {
        var invoice = Decrypt(await cosmo.GetInvoiceAsync(id, companyId)
            ?? throw new InvalidOperationException("Invoice not found."));

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be sent.");

        // Find the AR account
        var accounts = await coaService.GetChartOfAccountsAsync(companyId);
        var arAccount = accounts.FirstOrDefault(a => a.SubType == AccountSubType.AccountsReceivable)
            ?? throw new InvalidOperationException("No Accounts Receivable account found in chart of accounts.");

        // Build journal entry lines: DR AR for total, CR Revenue per line
        var jeLines = new List<JournalLine>
        {
            new()
            {
                AccountId = arAccount.id,
                AccountNumber = arAccount.AccountNumber,
                AccountName = arAccount.Name,
                Debit = invoice.Total,
                Credit = 0,
                Description = $"Invoice {invoice.InvoiceNumber} - {invoice.CustomerName}"
            }
        };

        foreach (var line in invoice.Lines)
        {
            jeLines.Add(new JournalLine
            {
                AccountId = line.AccountId,
                AccountNumber = line.AccountNumber,
                AccountName = line.AccountName,
                Debit = 0,
                Credit = line.Amount,
                Description = line.Description
            });
        }

        var journalEntry = new JournalEntry
        {
            CompanyId = companyId,
            EntryDate = invoice.InvoiceDate,
            Description = $"Invoice {invoice.InvoiceNumber} to {invoice.CustomerName}",
            SourceType = SourceType.Invoice,
            SourceId = invoice.id,
            Lines = jeLines
        };

        var je = await accounting.CreateJournalEntryAsync(journalEntry);
        await accounting.PostJournalEntryAsync(je.id, companyId);

        invoice.Status = InvoiceStatus.Sent;
        invoice.JournalEntryId = je.id;
        Encrypt(invoice);
        var sent = Decrypt(await cosmo.UpdateInvoiceAsync(invoice));
        await audit.LogAsync(companyId, "", "Sent", nameof(Invoice), id,
            sent.InvoiceNumber, $"Invoice sent to {sent.CustomerName} ({sent.Total:C})");
        return sent;
    }

    /// <summary>
    /// Updates an invoice that is still in Draft status.
    /// </summary>
    public async Task<Invoice> UpdateDraftInvoiceAsync(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be edited.");

        ValidateInvoice(invoice);

        for (int i = 0; i < invoice.Lines.Count; i++)
            invoice.Lines[i].LineNumber = i + 1;

        Encrypt(invoice);
        return Decrypt(await cosmo.UpdateInvoiceAsync(invoice));
    }

    /// <summary>
    /// Voids an invoice and its associated journal entry.
    /// </summary>
    public async Task<Invoice> VoidInvoiceAsync(string id, string companyId)
    {
        var invoice = Decrypt(await cosmo.GetInvoiceAsync(id, companyId)
            ?? throw new InvalidOperationException("Invoice not found."));

        if (invoice.Status == InvoiceStatus.Voided)
            throw new InvalidOperationException("Invoice is already voided.");

        if (!string.IsNullOrEmpty(invoice.JournalEntryId))
            await accounting.VoidJournalEntryAsync(invoice.JournalEntryId, companyId);

        invoice.Status = InvoiceStatus.Voided;
        Encrypt(invoice);
        return Decrypt(await cosmo.UpdateInvoiceAsync(invoice));
    }

    public async Task<Invoice?> GetInvoiceAsync(string id, string companyId)
    {
        var invoice = await cosmo.GetInvoiceAsync(id, companyId);
        return invoice != null ? Decrypt(invoice) : null;
    }

    public async Task<List<Invoice>> GetInvoicesAsync(string companyId, InvoiceStatus? status = null, string? customerId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var invoices = await cosmo.GetInvoicesAsync(companyId, status, customerId, fromDate, toDate);
        return DecryptAll(invoices);
    }

    public async Task<List<Invoice>> GetOpenInvoicesAsync(string companyId, string? customerId = null)
    {
        var invoices = await cosmo.GetOpenInvoicesAsync(companyId, customerId);
        return DecryptAll(invoices);
    }

    /// <summary>
    /// Applies a payment amount to an invoice and recalculates status.
    /// </summary>
    public async Task ApplyPaymentToInvoiceAsync(string invoiceId, string companyId, decimal paymentAmount)
    {
        var invoice = Decrypt(await cosmo.GetInvoiceAsync(invoiceId, companyId)
            ?? throw new InvalidOperationException("Invoice not found."));

        var newAmountPaid = invoice.AmountPaid + paymentAmount;
        var newBalanceDue = invoice.Total - newAmountPaid;

        InvoiceStatus newStatus;
        if (newBalanceDue <= 0)
            newStatus = InvoiceStatus.Paid;
        else if (newAmountPaid > 0)
            newStatus = InvoiceStatus.PartiallyPaid;
        else
            newStatus = invoice.Status;

        await cosmo.PatchInvoicePaymentAsync(invoiceId, companyId, newAmountPaid, newStatus);
    }

    /// <summary>
    /// Reverses a payment amount from an invoice and recalculates status.
    /// </summary>
    public async Task ReversePaymentFromInvoiceAsync(string invoiceId, string companyId, decimal paymentAmount)
    {
        var invoice = Decrypt(await cosmo.GetInvoiceAsync(invoiceId, companyId)
            ?? throw new InvalidOperationException("Invoice not found."));

        var newAmountPaid = Math.Max(0, invoice.AmountPaid - paymentAmount);
        var newBalanceDue = invoice.Total - newAmountPaid;

        InvoiceStatus newStatus;
        if (newAmountPaid == 0)
            newStatus = InvoiceStatus.Sent;
        else if (newBalanceDue > 0)
            newStatus = InvoiceStatus.PartiallyPaid;
        else
            newStatus = InvoiceStatus.Paid;

        await cosmo.PatchInvoicePaymentAsync(invoiceId, companyId, newAmountPaid, newStatus);
    }

    /// <summary>
    /// Finds invoices past their DueDate and updates status to Overdue.
    /// </summary>
    public async Task CheckOverdueInvoicesAsync(string companyId)
    {
        var openInvoices = await cosmo.GetOpenInvoicesAsync(companyId);
        DecryptAll(openInvoices);

        var today = DateTime.UtcNow.Date;
        foreach (var invoice in openInvoices)
        {
            if (invoice.DueDate < today && invoice.Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid)
            {
                await cosmo.PatchInvoicePaymentAsync(invoice.id, companyId, invoice.AmountPaid, InvoiceStatus.Overdue);
            }
        }
    }

    public static DateTime ComputeDueDate(DateTime invoiceDate, PaymentTerms terms)
    {
        return terms == PaymentTerms.DueOnReceipt
            ? invoiceDate
            : invoiceDate.AddDays((int)terms);
    }

    public static void ValidateInvoice(Invoice invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.CompanyId))
            throw new InvalidOperationException("Company ID is required.");
        if (string.IsNullOrWhiteSpace(invoice.CustomerId))
            throw new InvalidOperationException("Customer is required.");
        if (invoice.Lines == null || invoice.Lines.Count == 0)
            throw new InvalidOperationException("Invoice must have at least one line item.");
        if (invoice.Lines.Any(l => l.Quantity <= 0))
            throw new InvalidOperationException("Line item quantity must be greater than zero.");
        if (invoice.Lines.Any(l => l.UnitPrice < 0))
            throw new InvalidOperationException("Line item unit price cannot be negative.");
        if (invoice.Lines.Any(l => string.IsNullOrWhiteSpace(l.AccountId)))
            throw new InvalidOperationException("All line items must have a revenue account selected.");
        if (invoice.Lines.Any(l => string.IsNullOrWhiteSpace(l.Description)))
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
