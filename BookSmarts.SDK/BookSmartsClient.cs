using BookSmarts.SDK.Clients;

namespace BookSmarts.SDK;

/// <summary>
/// Main entry point for the BookSmarts SDK.
/// </summary>
public class BookSmartsClient : IDisposable
{
    private readonly HttpClient _http;

    public CompanyClient Companies { get; }
    public CustomerClient Customers { get; }
    public InvoiceClient Invoices { get; }
    public PaymentClient Payments { get; }
    public JournalEntryClient JournalEntries { get; }
    public ChartOfAccountsClient ChartOfAccounts { get; }

    public BookSmartsClient(string baseUrl, string clientKey)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        _http.DefaultRequestHeaders.Add("x-daisi-client-key", clientKey);

        Companies = new CompanyClient(_http);
        Customers = new CustomerClient(_http);
        Invoices = new InvoiceClient(_http);
        Payments = new PaymentClient(_http);
        JournalEntries = new JournalEntryClient(_http);
        ChartOfAccounts = new ChartOfAccountsClient(_http);
    }

    public BookSmartsClient(HttpClient httpClient)
    {
        _http = httpClient;
        Companies = new CompanyClient(_http);
        Customers = new CustomerClient(_http);
        Invoices = new InvoiceClient(_http);
        Payments = new PaymentClient(_http);
        JournalEntries = new JournalEntryClient(_http);
        ChartOfAccounts = new ChartOfAccountsClient(_http);
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
