using System.Net.Http.Json;
using BookSmarts.SDK.Models;

namespace BookSmarts.SDK.Clients;

public class CompanyClient(HttpClient http)
{
    public async Task<List<Company>> ListAsync()
    {
        return await http.GetFromJsonAsync<List<Company>>("/api/companies", BookSmartsClient.JsonOptions) ?? [];
    }

    public async Task<Company?> GetAsync(string id)
    {
        return await http.GetFromJsonAsync<Company>($"/api/companies/{id}", BookSmartsClient.JsonOptions);
    }
}
