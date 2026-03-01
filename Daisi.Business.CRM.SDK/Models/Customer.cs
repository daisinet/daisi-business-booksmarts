namespace Daisi.Business.CRM.SDK.Models;

public class Customer
{
    public string Id { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsLead { get; set; } = true;
    public DateTime? DateConverted { get; set; }
    public List<AttributeValue> Attributes { get; set; } = [];
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public string Status { get; set; } = "Active";
}

public class AttributeValue
{
    public string Key { get; set; } = "";
    public string? Value { get; set; }
}
