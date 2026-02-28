using BookSmarts.Core.Models;
using BookSmarts.Services;

namespace BookSmarts.Tests;

public class AgingReportTests
{
    [Theory]
    [InlineData(-5, "Current")]   // not yet due
    [InlineData(0, "Current")]    // due today
    [InlineData(1, "1-30")]
    [InlineData(15, "1-30")]
    [InlineData(30, "1-30")]
    [InlineData(31, "31-60")]
    [InlineData(60, "31-60")]
    [InlineData(61, "61-90")]
    [InlineData(90, "61-90")]
    [InlineData(91, "90+")]
    [InlineData(180, "90+")]
    public void GetAgingBucket_ReturnsCorrectBucket(int daysOutstanding, string expectedBucket)
    {
        var bucket = AgingService.GetAgingBucket(daysOutstanding);
        Assert.Equal(expectedBucket, bucket);
    }

    [Fact]
    public void BuildSummaries_GroupsByContact()
    {
        var lines = new List<AgingReportLine>
        {
            new() { ContactId = "c1", ContactName = "Customer A", BalanceDue = 100, AgingBucket = "Current" },
            new() { ContactId = "c1", ContactName = "Customer A", BalanceDue = 200, AgingBucket = "1-30" },
            new() { ContactId = "c2", ContactName = "Customer B", BalanceDue = 300, AgingBucket = "31-60" },
        };

        var summaries = AgingService.BuildSummaries(lines);

        Assert.Equal(2, summaries.Count);

        var a = summaries.First(s => s.ContactId == "c1");
        Assert.Equal(100m, a.Current);
        Assert.Equal(200m, a.Days1To30);
        Assert.Equal(0m, a.Days31To60);
        Assert.Equal(300m, a.Total);

        var b = summaries.First(s => s.ContactId == "c2");
        Assert.Equal(0m, b.Current);
        Assert.Equal(300m, b.Days31To60);
        Assert.Equal(300m, b.Total);
    }

    [Fact]
    public void BuildSummaries_OrdersByContactName()
    {
        var lines = new List<AgingReportLine>
        {
            new() { ContactId = "c2", ContactName = "Zebra Corp", BalanceDue = 100, AgingBucket = "Current" },
            new() { ContactId = "c1", ContactName = "Alpha Inc", BalanceDue = 200, AgingBucket = "1-30" },
        };

        var summaries = AgingService.BuildSummaries(lines);

        Assert.Equal("Alpha Inc", summaries[0].ContactName);
        Assert.Equal("Zebra Corp", summaries[1].ContactName);
    }

    [Fact]
    public void BuildSummaries_EmptyLines_ReturnsEmpty()
    {
        var summaries = AgingService.BuildSummaries(new List<AgingReportLine>());
        Assert.Empty(summaries);
    }

    [Fact]
    public void AgingReportSummary_Total_SumsAllBuckets()
    {
        var summary = new AgingReportSummary
        {
            Current = 100,
            Days1To30 = 200,
            Days31To60 = 300,
            Days61To90 = 400,
            Days90Plus = 500
        };
        Assert.Equal(1500m, summary.Total);
    }

    [Fact]
    public void BuildSummaries_AllBuckets_PopulatedCorrectly()
    {
        var lines = new List<AgingReportLine>
        {
            new() { ContactId = "c1", ContactName = "Test", BalanceDue = 10, AgingBucket = "Current" },
            new() { ContactId = "c1", ContactName = "Test", BalanceDue = 20, AgingBucket = "1-30" },
            new() { ContactId = "c1", ContactName = "Test", BalanceDue = 30, AgingBucket = "31-60" },
            new() { ContactId = "c1", ContactName = "Test", BalanceDue = 40, AgingBucket = "61-90" },
            new() { ContactId = "c1", ContactName = "Test", BalanceDue = 50, AgingBucket = "90+" },
        };

        var summaries = AgingService.BuildSummaries(lines);
        var s = summaries.Single();

        Assert.Equal(10m, s.Current);
        Assert.Equal(20m, s.Days1To30);
        Assert.Equal(30m, s.Days31To60);
        Assert.Equal(40m, s.Days61To90);
        Assert.Equal(50m, s.Days90Plus);
        Assert.Equal(150m, s.Total);
    }
}
