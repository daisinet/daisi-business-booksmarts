namespace BookSmarts.Core.Models;

public class AgingReportLine
{
    public string DocumentId { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactId { get; set; } = "";
    public DateTime DocumentDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal BalanceDue { get; set; }
    public int DaysOutstanding { get; set; }
    public string AgingBucket { get; set; } = "";
}

public class AgingReportSummary
{
    public string ContactName { get; set; } = "";
    public string ContactId { get; set; } = "";
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + Days90Plus;
}
