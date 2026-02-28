using System.Net.Http.Json;
using BookSmarts.SDK.Models;

namespace BookSmarts.SDK.Clients;

public class JournalEntryClient(HttpClient http)
{
    public async Task<List<JournalEntry>> ListAsync(string companyId, string? status = null)
    {
        var url = $"/api/companies/{companyId}/journal-entries";
        if (!string.IsNullOrEmpty(status))
            url += $"?status={Uri.EscapeDataString(status)}";
        return await http.GetFromJsonAsync<List<JournalEntry>>(url) ?? [];
    }

    public async Task<JournalEntry?> GetAsync(string companyId, string id)
    {
        return await http.GetFromJsonAsync<JournalEntry>($"/api/companies/{companyId}/journal-entries/{id}");
    }

    public async Task<JournalEntry> CreateAsync(JournalEntry entry)
    {
        var response = await http.PostAsJsonAsync($"/api/companies/{entry.CompanyId}/journal-entries", entry);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JournalEntry>())!;
    }

    public async Task<JournalEntry> PostAsync(string companyId, string id)
    {
        var response = await http.PostAsync($"/api/companies/{companyId}/journal-entries/{id}/post", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JournalEntry>())!;
    }

    public async Task<JournalEntry> VoidAsync(string companyId, string id)
    {
        var response = await http.PostAsync($"/api/companies/{companyId}/journal-entries/{id}/void", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JournalEntry>())!;
    }
}
