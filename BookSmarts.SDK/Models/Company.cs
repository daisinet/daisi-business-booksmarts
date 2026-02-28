namespace BookSmarts.SDK.Models;

public class Company
{
    public string Id { get; set; } = "";
    public string OrganizationId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "USD";
    public string? TaxId { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
