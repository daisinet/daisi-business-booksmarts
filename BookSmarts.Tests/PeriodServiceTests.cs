using BookSmarts.Core.Models;

namespace BookSmarts.Tests;

public class PeriodServiceTests
{
    [Fact]
    public void FiscalYear_Periods_CanBeGenerated()
    {
        var fy = new FiscalYear
        {
            CompanyId = "co-test",
            Name = "FY 2026",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };

        // Verify the model structure supports 12 periods
        for (int i = 1; i <= 12; i++)
        {
            var start = new DateTime(2026, i, 1);
            fy.Periods.Add(new FiscalPeriod
            {
                PeriodId = $"fp-{i}",
                PeriodNumber = i,
                Name = start.ToString("MMMM yyyy"),
                StartDate = start,
                EndDate = start.AddMonths(1).AddDays(-1)
            });
        }

        Assert.Equal(12, fy.Periods.Count);
        Assert.Equal("January 2026", fy.Periods[0].Name);
        Assert.Equal("December 2026", fy.Periods[11].Name);
        Assert.Equal(new DateTime(2026, 1, 31), fy.Periods[0].EndDate);
        Assert.Equal(new DateTime(2026, 12, 31), fy.Periods[11].EndDate);
    }

    [Fact]
    public void FiscalPeriod_DefaultStatus_IsOpen()
    {
        var period = new FiscalPeriod();
        Assert.Equal(Core.Enums.FiscalPeriodStatus.Open, period.Status);
    }
}
