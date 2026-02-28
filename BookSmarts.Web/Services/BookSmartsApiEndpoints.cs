using BookSmarts.Core.Enums;
using BookSmarts.Core.Models;
using BookSmarts.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookSmarts.Web.Services;

public static class BookSmartsApiEndpoints
{
    public static void MapBookSmartsApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").AddEndpointFilter<ApiKeyAuthFilter>();

        // ── Companies ──

        api.MapGet("/companies", async (OrganizationService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            var companies = await svc.GetCompaniesAsync(accountId!);
            return Results.Ok(companies);
        });

        api.MapGet("/companies/{companyId}", async (string companyId, OrganizationService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            var company = await svc.GetCompanyAsync(companyId, accountId!);
            return company != null ? Results.Ok(company) : Results.NotFound();
        });

        // ── Customers ──

        api.MapGet("/companies/{companyId}/customers", async (string companyId, OrganizationService orgSvc, ContactService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var customers = await svc.GetCustomersAsync(companyId);
            return Results.Ok(customers);
        });

        api.MapGet("/companies/{companyId}/customers/{id}", async (string companyId, string id, OrganizationService orgSvc, ContactService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var customer = await svc.GetCustomerAsync(id, companyId);
            return customer != null ? Results.Ok(customer) : Results.NotFound();
        });

        api.MapPost("/companies/{companyId}/customers", async (string companyId, [FromBody] Customer customer, OrganizationService orgSvc, ContactService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            customer.CompanyId = companyId;
            var created = await svc.CreateCustomerAsync(customer);
            return Results.Created($"/api/companies/{companyId}/customers/{created.id}", created);
        });

        api.MapPut("/companies/{companyId}/customers/{id}", async (string companyId, string id, [FromBody] Customer customer, OrganizationService orgSvc, ContactService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            customer.id = id;
            customer.CompanyId = companyId;
            var updated = await svc.UpdateCustomerAsync(customer);
            return Results.Ok(updated);
        });

        // ── Invoices ──

        api.MapGet("/companies/{companyId}/invoices", async (string companyId, OrganizationService orgSvc, InvoiceService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();

            InvoiceStatus? status = null;
            if (ctx.Request.Query.TryGetValue("status", out var statusVal) && Enum.TryParse<InvoiceStatus>(statusVal, true, out var parsed))
                status = parsed;
            var customerId = ctx.Request.Query["customerId"].FirstOrDefault();

            var invoices = await svc.GetInvoicesAsync(companyId, status, customerId);
            return Results.Ok(invoices);
        });

        api.MapGet("/companies/{companyId}/invoices/open", async (string companyId, OrganizationService orgSvc, InvoiceService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var customerId = ctx.Request.Query["customerId"].FirstOrDefault();
            var invoices = await svc.GetOpenInvoicesAsync(companyId, customerId);
            return Results.Ok(invoices);
        });

        api.MapGet("/companies/{companyId}/invoices/{id}", async (string companyId, string id, OrganizationService orgSvc, InvoiceService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var invoice = await svc.GetInvoiceAsync(id, companyId);
            return invoice != null ? Results.Ok(invoice) : Results.NotFound();
        });

        api.MapPost("/companies/{companyId}/invoices", async (string companyId, [FromBody] Invoice invoice, OrganizationService orgSvc, InvoiceService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            invoice.CompanyId = companyId;
            var created = await svc.CreateInvoiceAsync(invoice);
            return Results.Created($"/api/companies/{companyId}/invoices/{created.id}", created);
        });

        api.MapPost("/companies/{companyId}/invoices/{id}/send", async (string companyId, string id, OrganizationService orgSvc, InvoiceService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var sent = await svc.SendInvoiceAsync(id, companyId);
            return Results.Ok(sent);
        });

        api.MapPost("/companies/{companyId}/invoices/{id}/void", async (string companyId, string id, OrganizationService orgSvc, InvoiceService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var voided = await svc.VoidInvoiceAsync(id, companyId);
            return Results.Ok(voided);
        });

        // ── Payments ──

        api.MapGet("/companies/{companyId}/payments", async (string companyId, OrganizationService orgSvc, PaymentService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();

            PaymentType? type = null;
            if (ctx.Request.Query.TryGetValue("type", out var typeVal) && Enum.TryParse<PaymentType>(typeVal, true, out var parsedType))
                type = parsedType;
            PaymentStatus? status = null;
            if (ctx.Request.Query.TryGetValue("status", out var statusVal) && Enum.TryParse<PaymentStatus>(statusVal, true, out var parsedStatus))
                status = parsedStatus;

            var payments = await svc.GetPaymentsAsync(companyId, type, status);
            return Results.Ok(payments);
        });

        api.MapGet("/companies/{companyId}/payments/{id}", async (string companyId, string id, OrganizationService orgSvc, PaymentService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var payment = await svc.GetPaymentAsync(id, companyId);
            return payment != null ? Results.Ok(payment) : Results.NotFound();
        });

        api.MapPost("/companies/{companyId}/payments/receive", async (string companyId, [FromBody] Payment payment, OrganizationService orgSvc, PaymentService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            payment.CompanyId = companyId;
            var created = await svc.ReceiveCustomerPaymentAsync(payment);
            return Results.Created($"/api/companies/{companyId}/payments/{created.id}", created);
        });

        api.MapPost("/companies/{companyId}/payments/{id}/void", async (string companyId, string id, OrganizationService orgSvc, PaymentService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var voided = await svc.VoidPaymentAsync(id, companyId);
            return Results.Ok(voided);
        });

        // ── Journal Entries ──

        api.MapGet("/companies/{companyId}/journal-entries", async (string companyId, OrganizationService orgSvc, AccountingService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();

            JournalEntryStatus? status = null;
            if (ctx.Request.Query.TryGetValue("status", out var statusVal) && Enum.TryParse<JournalEntryStatus>(statusVal, true, out var parsed))
                status = parsed;

            var entries = await svc.GetJournalEntriesAsync(companyId, status);
            return Results.Ok(entries);
        });

        api.MapGet("/companies/{companyId}/journal-entries/{id}", async (string companyId, string id, OrganizationService orgSvc, AccountingService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var entry = await svc.GetJournalEntryAsync(id, companyId);
            return entry != null ? Results.Ok(entry) : Results.NotFound();
        });

        api.MapPost("/companies/{companyId}/journal-entries", async (string companyId, [FromBody] JournalEntry entry, OrganizationService orgSvc, AccountingService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            entry.CompanyId = companyId;
            var created = await svc.CreateJournalEntryAsync(entry);
            return Results.Created($"/api/companies/{companyId}/journal-entries/{created.id}", created);
        });

        api.MapPost("/companies/{companyId}/journal-entries/{id}/post", async (string companyId, string id, OrganizationService orgSvc, AccountingService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var posted = await svc.PostJournalEntryAsync(id, companyId);
            return Results.Ok(posted);
        });

        api.MapPost("/companies/{companyId}/journal-entries/{id}/void", async (string companyId, string id, OrganizationService orgSvc, AccountingService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var voided = await svc.VoidJournalEntryAsync(id, companyId);
            return Results.Ok(voided);
        });

        // ── Chart of Accounts ──

        api.MapGet("/companies/{companyId}/accounts", async (string companyId, OrganizationService orgSvc, ChartOfAccountsService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            bool activeOnly = !ctx.Request.Query.TryGetValue("activeOnly", out var val) || !bool.TryParse(val, out var ao) || ao;
            var accounts = await svc.GetChartOfAccountsAsync(companyId, activeOnly);
            return Results.Ok(accounts);
        });

        api.MapGet("/companies/{companyId}/accounts/{id}", async (string companyId, string id, OrganizationService orgSvc, ChartOfAccountsService svc, HttpContext ctx) =>
        {
            var accountId = ctx.Items["accountId"] as string;
            if (await orgSvc.GetCompanyAsync(companyId, accountId!) is null)
                return Results.Forbid();
            var account = await svc.GetAccountEntryAsync(id, companyId);
            return account != null ? Results.Ok(account) : Results.NotFound();
        });
    }
}
