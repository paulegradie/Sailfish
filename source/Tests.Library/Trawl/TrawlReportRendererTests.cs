using System;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.Trawl;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

public class TrawlReportRendererTests
{
    private static TrawlResult SampleResult() => new()
    {
        DisplayName = "My.Load(scenario: 1)",
        Model = LoadModel.OpenModel,
        VirtualUsers = 8,
        Duration = TimeSpan.FromSeconds(3),
        TotalRequests = 300,
        TotalErrors = 6,
        RequestsPerSecond = 100,
        ErrorRate = 0.02,
        Latency = new LatencyStats { Min = 1, Mean = 6, P50 = 5, P75 = 8, P90 = 12, P95 = 18, P99 = 40, Max = 80 },
        LatencySamplesMs = new[] { 1.0, 2, 3, 5, 8, 12, 18, 40, 80 },
        TimeSeries = new TrawlTimeSeries
        {
            SecondOffsets = new double[] { 0, 1, 2 },
            RequestsPerSecond = new double[] { 90, 100, 110 },
            P99Ms = new double[] { 30, 40, 50 }
        }
    };

    [Fact]
    public void Render_ContainsHeadlineFiguresAndTimeSeries()
    {
        var report = TrawlReportRenderer.Render(SampleResult(), DistributionPlotStyle.Histogram);

        report.ShouldContain("Trawl — My.Load(scenario: 1)");
        report.ShouldContain("req/s");
        report.ShouldContain("p75"); // the p75 percentile is now surfaced in the table
        report.ShouldContain("p99");
        report.ShouldContain("Throughput/s");
    }

    [Fact]
    public void Sparkline_FlatInput_IsLowestBar()
    {
        TrawlReportRenderer.Sparkline(new[] { 0.0, 0.0, 0.0 }).ShouldBe("▁▁▁");
    }

    [Fact]
    public void Sparkline_Ascending_RisesToFull()
    {
        var spark = TrawlReportRenderer.Sparkline(new[] { 0.0, 1, 2, 3, 4, 5, 6, 7 });
        spark[0].ShouldBe('▁');
        spark[^1].ShouldBe('█');
    }
}
