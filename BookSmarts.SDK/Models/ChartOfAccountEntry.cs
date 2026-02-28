namespace BookSmarts.SDK.Models;

public class ChartOfAccountEntry
{
    public string Id { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Category { get; set; } = "";
    public string SubType { get; set; } = "";
    public string NormalBalance { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool IsSystemAccount { get; set; }
    public string? ParentAccountId { get; set; }
}
