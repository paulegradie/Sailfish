using System.Linq;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Presentation;

public class NiceAxisTests
{
    [Fact]
    public void Compute_RoundsObservedRangeOutToNiceBounds()
    {
        var axis = NiceAxis.Compute(50.649, 51.310);

        axis.Min.ShouldBe(50.6, 1e-9);
        axis.Max.ShouldBe(51.4, 1e-9);
        axis.Step.ShouldBe(0.2, 1e-9);
        axis.Decimals.ShouldBe(1);
        axis.Ticks.ShouldContain(t => System.Math.Abs(t - 50.8) < 1e-9);
        axis.Ticks.ShouldContain(t => System.Math.Abs(t - 51.0) < 1e-9);
        axis.Ticks.ShouldContain(t => System.Math.Abs(t - 51.2) < 1e-9);
    }

    [Fact]
    public void Compute_ProducesRoundLabelsNotRawValues()
    {
        var axis = NiceAxis.Compute(50.649, 51.310);
        var labels = axis.Ticks.Select(t => t.ToString("F" + axis.Decimals, System.Globalization.CultureInfo.InvariantCulture)).ToList();

        labels.ShouldContain("50.6");
        labels.ShouldContain("51.4");
        labels.ShouldNotContain("50.649");
    }

    [Fact]
    public void Compute_IdenticalValues_ProducesWindowAroundValue()
    {
        var axis = NiceAxis.Compute(51.0, 51.0);

        axis.Min.ShouldBeLessThan(51.0);
        axis.Max.ShouldBeGreaterThan(51.0);
        axis.Ticks.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Compute_WiderRange_UsesCoarserStepAndZeroDecimals()
    {
        var axis = NiceAxis.Compute(1.0, 12.0);

        axis.Step.ShouldBeGreaterThanOrEqualTo(1.0);
        axis.Decimals.ShouldBe(0);
        axis.Min.ShouldBeLessThanOrEqualTo(1.0);
        axis.Max.ShouldBeGreaterThanOrEqualTo(12.0);
    }
}
