namespace BookSmarts.SDK.Models;

public class Customer
{
    public string Id { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string DefaultPaymentTerms { get; set; } = "Net30";
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
