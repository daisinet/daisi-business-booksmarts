using System.Net.Http.Json;
using Daisi.Business.CRM.SDK.Models;

namespace Daisi.Business.CRM.SDK.Clients;

public class CustomerClient(HttpClient http)
{
    public async Task<Customer> CreateAsync(Customer customer)
    {
        var response = await http.PostAsJsonAsync("/api/customers", customer);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Customer>() ?? customer;
    }
}
