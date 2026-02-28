using System.Net.Http.Json;
using BookSmarts.SDK.Models;

namespace BookSmarts.SDK.Clients;

public class InvoiceClient(HttpClient http)
{
    public async Task<List<Invoice>> ListAsync(string companyId, string? status = null, string? customerId = null)
    {
        var url = $"/api/companies/{companyId}/invoices?";
        if (!string.IsNullOrEmpty(status))
            url += $"status={Uri.EscapeDataString(status)}&";
        if (!string.IsNullOrEmpty(customerId))
            url += $"customerId={Uri.EscapeDataString(customerId)}&";
        return await http.GetFromJsonAsync<List<Invoice>>(url.TrimEnd('&', '?')) ?? [];
    }

    public async Task<Invoice?> GetAsync(string companyId, string id)
    {
        return await http.GetFromJsonAsync<Invoice>($"/api/companies/{companyId}/invoices/{id}");
    }

    public async Task<List<Invoice>> GetOpenAsync(string companyId, string? customerId = null)
    {
        var url = $"/api/companies/{companyId}/invoices/open";
        if (!string.IsNullOrEmpty(customerId))
            url += $"?customerId={Uri.EscapeDataString(customerId)}";
        return await http.GetFromJsonAsync<List<Invoice>>(url) ?? [];
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        var response = await http.PostAsJsonAsync($"/api/companies/{invoice.CompanyId}/invoices", invoice);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Invoice>())!;
    }

    public async Task<Invoice> SendAsync(string companyId, string id)
    {
        var response = await http.PostAsync($"/api/companies/{companyId}/invoices/{id}/send", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Invoice>())!;
    }

    public async Task<Invoice> VoidAsync(string companyId, string id)
    {
        var response = await http.PostAsync($"/api/companies/{companyId}/invoices/{id}/void", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Invoice>())!;
    }
}
