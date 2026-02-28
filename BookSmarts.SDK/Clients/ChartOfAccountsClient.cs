using System.Net.Http.Json;
using BookSmarts.SDK.Models;

namespace BookSmarts.SDK.Clients;

public class ChartOfAccountsClient(HttpClient http)
{
    public async Task<List<ChartOfAccountEntry>> ListAsync(string companyId, bool activeOnly = true)
    {
        return await http.GetFromJsonAsync<List<ChartOfAccountEntry>>(
            $"/api/companies/{companyId}/accounts?activeOnly={activeOnly}") ?? [];
    }

    public async Task<ChartOfAccountEntry?> GetAsync(string companyId, string id)
    {
        return await http.GetFromJsonAsync<ChartOfAccountEntry>(
            $"/api/companies/{companyId}/accounts/{id}");
    }
}
