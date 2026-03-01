using Daisi.Business.CRM.SDK.Clients;

namespace Daisi.Business.CRM.SDK;

public class CrmClient : IDisposable
{
    private readonly HttpClient _http;

    public CustomerClient Customers { get; }

    public CrmClient(string baseUrl, string clientKey)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        _http.DefaultRequestHeaders.Add("x-daisi-client-key", clientKey);

        Customers = new CustomerClient(_http);
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
