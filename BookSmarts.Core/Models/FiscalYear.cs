using BookSmarts.Core.Encryption;
using BookSmarts.Core.Enums;

namespace BookSmarts.Core.Models;

public class FiscalYear : IEncryptable
{
    public string id { get; set; } = "";
    public string Type { get; set; } = nameof(FiscalYear);
    public string CompanyId { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public List<FiscalPeriod> Periods { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? EncryptedPayload { get; set; }
}

public class FiscalPeriod
{
    public string PeriodId { get; set; } = "";
    public int PeriodNumber { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public FiscalPeriodStatus Status { get; set; } = FiscalPeriodStatus.Open;
    public DateTime? ClosedUtc { get; set; }
}
