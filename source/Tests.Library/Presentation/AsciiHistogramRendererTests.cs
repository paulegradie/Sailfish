using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Presentation;

public class AsciiHistogramRendererTests
{
    private static DistributionData Dist(string label, IReadOnlyList<double> samples, int outliers = 0)
        => DistributionData.FromSamples(label, samples, double.NaN, double.NaN, outliers);

    // A unimodal-ish sample centred near `centre`.
    private static double[] Cluster(double centre, double spread, int n)
        => Enumerable.Range(0, n).Select(i => centre + spread * Math.Sin(i * 2.3)).ToArray();

    [Fact]
    public void Render_SingleDistribution_UsesHistogramGlyphsAndDropsOldBoxStyle()
    {
        var output = AsciiHistogramRenderer.Render(new[] { Dist("current", Cluster(51.0, 0.3, 200), 83) }, DurationUnit.Milliseconds);

        output.IndexOfAny("▁▂▃▄▅▆▇█".ToCharArray()).ShouldBeGreaterThanOrEqualTo(0); // histogram glyphs present
        output.ShouldContain("count per bin");
        // Old inline box-plot glyphs must be gone.
        output.ShouldNotContain("◆");
        output.ShouldNotContain("┃");
        output.ShouldNotContain("▓");
    }

    [Fact]
    public void Render_TwoDistributions_ShareOneAxis()
    {
        var fast = Dist("fast run", Cluster(50.7, 0.15, 200));
        var slow = Dist("slow run", Cluster(51.2, 0.15, 200));

        var output = AsciiHistogramRenderer.Render(new[] { fast, slow }, DurationUnit.Milliseconds);

        output.ShouldContain("fast run");
        output.ShouldContain("slow run");
        // Exactly one shared axis line (the line carrying the left cap).
        output.Split('\n').Count(l => l.Contains('├')).ShouldBe(1);
    }

    [Fact]
    public void Render_FourDistributions_AlignHistogramRegion()
    {
        var dists = new[]
        {
            Dist("cold start", Cluster(50.85, 0.12, 200), 83),
            Dist("warm start", Cluster(50.95, 0.10, 200), 22),
            Dist("cached", Cluster(50.92, 0.08, 200), 10),
            Dist("loaded system", Cluster(51.15, 0.10, 200), 91),
        };

        var output = AsciiHistogramRenderer.Render(dists, DurationUnit.Milliseconds);
        var summaryLines = output.Split('\n').Where(l => l.Contains("n=")).ToList();

        summaryLines.Count.ShouldBe(4);
        // Labels are padded to a common width, so "n=" starts in the same column on every summary row.
        summaryLines.Select(l => l.IndexOf("n=", StringComparison.Ordinal)).Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public void Render_SummaryReportsExactMinMaxAndOutlierCount()
    {
        var samples = new[] { 50.649, 50.8, 51.0, 51.2, 51.310 };
        var output = AsciiHistogramRenderer.Render(new[] { Dist("m", samples, 83) }, DurationUnit.Milliseconds);

        output.ShouldContain("outliers=83");
        output.ShouldContain("min=50.649");
        output.ShouldContain("max=51.310");
    }

    [Fact]
    public void Render_UsesRoundedAxisTicks_NotRawValues()
    {
        var samples = new[] { 50.649, 50.811, 50.973, 51.148, 51.310 };
        var output = AsciiHistogramRenderer.Render(new[] { Dist("m", samples) }, DurationUnit.Milliseconds);

        output.ShouldContain("51.0");      // a rounded interior tick
        output.ShouldContain("50.6");      // rounded endpoint
        // raw values appear only in the summary line, never as an axis tick label
        var axisAndTickLines = output.Split('\n').Where(l => l.Contains('├') || (l.Contains('.') && !l.Contains("n="))).ToList();
        axisAndTickLines.Any(l => l.Contains("50.973")).ShouldBeFalse();
    }

    [Fact]
    public void Render_RightSkewed_ShowsSeparateMeanAndMedianMarkers()
    {
        // Many low values + a few high ones => mean noticeably greater than median.
        var samples = Enumerable.Repeat(1.0, 80).Concat(Enumerable.Repeat(9.0, 20)).ToArray();
        var output = AsciiHistogramRenderer.Render(new[] { Dist("skew", samples) }, DurationUnit.Milliseconds);

        output.ShouldContain("╵"); // mean
        output.ShouldContain("╿"); // median
    }

    [Fact]
    public void Render_EmptyInput_ReturnsEmpty()
    {
        AsciiHistogramRenderer.Render(Array.Empty<DistributionData>(), DurationUnit.Milliseconds).ShouldBeEmpty();
        AsciiHistogramRenderer.Render(new[] { Dist("x", Array.Empty<double>()) }, DurationUnit.Milliseconds).ShouldBeEmpty();
    }

    [Fact]
    public void Render_IdenticalValues_ProducesCentredSpikeWithoutCrashing()
    {
        var output = AsciiHistogramRenderer.Render(new[] { Dist("flat", Enumerable.Repeat(51.0, 50).ToArray()) }, DurationUnit.Milliseconds);

        output.ShouldNotBeNullOrEmpty();
        output.ShouldContain("█");          // a full-height spike
        output.ShouldContain("min=51.000");
        output.ShouldContain("max=51.000");
    }

    [Fact]
    public void Render_SubMillisecondData_AutoScalesAxisUnit()
    {
        var samples = Enumerable.Range(0, 60).Select(i => 0.0010 + i * 0.00001).ToArray();
        var unit = DurationFormatter.SelectUnit(samples);
        var output = AsciiHistogramRenderer.Render(new[] { Dist("fast", samples) }, unit);

        unit.ShouldBe(DurationUnit.Microseconds);
        output.ShouldContain("Time (µs)");
    }

    [Fact]
    public void Render_LongLabel_IsTruncatedAndAlignmentHolds()
    {
        var dists = new[]
        {
            Dist("a", Cluster(51.0, 0.2, 100)),
            Dist("a-really-long-distribution-name-that-overflows", Cluster(51.1, 0.2, 100)),
        };

        var output = AsciiHistogramRenderer.Render(dists, DurationUnit.Milliseconds);
        var summaryLines = output.Split('\n').Where(l => l.Contains("n=")).ToList();

        summaryLines.Count.ShouldBe(2);
        summaryLines.Select(l => l.IndexOf("n=", StringComparison.Ordinal)).Distinct().Count().ShouldBe(1);
    }
}
