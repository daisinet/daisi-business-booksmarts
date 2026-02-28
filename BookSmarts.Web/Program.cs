using System.Text.Json.Serialization;
using BookSmarts.Core.Models;
using BookSmarts.Data;
using BookSmarts.Services;
using BookSmarts.Web.Components;
using BookSmarts.Web.Services;
using Daisi.SDK.Models;
using Daisi.SDK.Web.Extensions;
using Going.Plaid;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddHttpClient();

// Daisi SSO authentication
builder.Services.AddDaisiForWeb()
                .AddDaisiMiddleware()
                .AddDaisiCookieKeyProvider();

// Plaid banking integration
builder.Services.AddPlaid(builder.Configuration);

// BookSmarts services
builder.Services.AddSingleton<BookSmartsCosmo>(sp =>
    new BookSmartsCosmo(builder.Configuration));
builder.Services.AddScoped<EncryptionContext>();
builder.Services.AddScoped<EncryptionSetupService>();
builder.Services.AddScoped<EncryptionMigrationService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<ChartOfAccountsService>();
builder.Services.AddScoped<AccountingService>();
builder.Services.AddScoped<PeriodService>();
builder.Services.AddScoped<BankingService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<BillService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<AgingService>();
builder.Services.AddScoped<FinancialStatementService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<InterCompanyService>();
builder.Services.AddScoped<ConsolidationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<CustomReportService>();

// AI services
builder.Services.AddScoped<FinancialContextBuilder>();
builder.Services.AddScoped<BookSmartsInferenceService>();

// JSON enum serialization for API endpoints
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();
app.UseDaisiMiddleware();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// BookSmarts API endpoints (client-key authenticated)
app.MapBookSmartsApiEndpoints();

// Plaid webhook endpoint — just flags that sync data is available.
// Actual sync happens when the user logs in and enters their PIN.
app.MapPost("/api/plaid/webhook", async (PlaidWebhookPayload payload, IServiceScopeFactory scopeFactory, ILogger<Program> logger) =>
{
    if (payload.WebhookType == "TRANSACTIONS" && payload.WebhookCode == "SYNC_UPDATES_AVAILABLE" && !string.IsNullOrEmpty(payload.ItemId))
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var cosmo = scope.ServiceProvider.GetRequiredService<BookSmartsCosmo>();

            var connection = await cosmo.GetBankConnectionByPlaidItemIdAsync(payload.ItemId);
            if (connection != null)
            {
                await cosmo.PatchBankConnectionWebhookAsync(connection.id, connection.CompanyId);
                logger.LogInformation("Webhook: flagged sync available for connection {ConnectionId}.", connection.id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook: error processing webhook for item {ItemId}.", payload.ItemId);
        }
    }

    return Results.Ok();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

DaisiStaticSettings.LoadFromConfiguration(builder.Configuration.AsEnumerable().ToDictionary(keySelector: x => x.Key, elementSelector: x => x.Value));

app.Run();
