using System.Net.Http.Json;
using BookSmarts.SDK.Models;

namespace BookSmarts.SDK.Clients;

public class CustomerClient(HttpClient http)
{
    public async Task<List<Customer>> ListAsync(string companyId)
    {
        return await http.GetFromJsonAsync<List<Customer>>($"/api/companies/{companyId}/customers", BookSmartsClient.JsonOptions) ?? [];
    }

    public async Task<Customer?> GetAsync(string companyId, string id)
    {
        return await http.GetFromJsonAsync<Customer>($"/api/companies/{companyId}/customers/{id}", BookSmartsClient.JsonOptions);
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        var response = await http.PostAsJsonAsync($"/api/companies/{customer.CompanyId}/customers", customer, BookSmartsClient.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Customer>(BookSmartsClient.JsonOptions))!;
    }

    public async Task<Customer> UpdateAsync(string companyId, string id, Customer customer)
    {
        var response = await http.PutAsJsonAsync($"/api/companies/{companyId}/customers/{id}", customer, BookSmartsClient.JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Customer>(BookSmartsClient.JsonOptions))!;
    }
}
