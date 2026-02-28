using System.Net.Http.Json;
using BookSmarts.SDK.Models;

namespace BookSmarts.SDK.Clients;

public class PaymentClient(HttpClient http)
{
    public async Task<List<Payment>> ListAsync(string companyId, string? type = null, string? status = null)
    {
        var url = $"/api/companies/{companyId}/payments?";
        if (!string.IsNullOrEmpty(type))
            url += $"type={Uri.EscapeDataString(type)}&";
        if (!string.IsNullOrEmpty(status))
            url += $"status={Uri.EscapeDataString(status)}&";
        return await http.GetFromJsonAsync<List<Payment>>(url.TrimEnd('&', '?')) ?? [];
    }

    public async Task<Payment?> GetAsync(string companyId, string id)
    {
        return await http.GetFromJsonAsync<Payment>($"/api/companies/{companyId}/payments/{id}");
    }

    public async Task<Payment> ReceiveCustomerPaymentAsync(Payment payment)
    {
        var response = await http.PostAsJsonAsync($"/api/companies/{payment.CompanyId}/payments/receive", payment);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Payment>())!;
    }

    public async Task<Payment> VoidAsync(string companyId, string id)
    {
        var response = await http.PostAsync($"/api/companies/{companyId}/payments/{id}/void", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Payment>())!;
    }
}
