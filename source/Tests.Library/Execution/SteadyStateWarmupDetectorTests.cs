using System.Collections.Generic;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class SteadyStateWarmupDetectorTests
{
    // Single source of truth: the production tuning values defined on the detector.
    private const int Window = SteadyStateWarmupDetector.DefaultWindow;
    private const double MaxDrift = SteadyStateWarmupDetector.DefaultMaxRelativeDrift;
    private const double MaxCv = SteadyStateWarmupDetector.DefaultMaxCoefficientOfVariation;

    private static SteadyStateWarmupResult Check(IReadOnlyList<double> xs) =>
        new SteadyStateWarmupDetector().Check(xs, Window, MaxDrift, MaxCv);

    [Fact]
    public void Stable_Flat_IsSteady()
    {
        Check(new double[] { 10, 10, 10, 10, 10, 10 }).ReachedSteadyState.ShouldBeTrue();
    }

    [Fact]
    public void DecreasingThenFlat_OnceWindowClearsTheTransient_IsSteady()
    {
        // JIT-tiering shape: high then falling, then flat. Once the recent window is past the
        // transient, it should read as steady.
        var r = Check(new double[] { 100, 80, 40, 20, 10, 10, 10, 10, 10, 10 });
        r.ReachedSteadyState.ShouldBeTrue();
    }

    [Fact]
    public void StillTrendingDown_NotSteady()
    {
        Check(new double[] { 60, 50, 40, 30, 20, 10 }).ReachedSteadyState.ShouldBeFalse();
    }

    [Fact]
    public void FlatTrendButHighDispersion_NotSteady()
    {
        // Prior/recent medians match (drift ~0) but the window is noisy → CV gate rejects it.
        var r = Check(new double[] { 20, 20, 20, 20, 40, 0 });
        r.ReachedSteadyState.ShouldBeFalse();
        r.RelativeDrift.ShouldBeLessThan(MaxDrift);          // trend looks flat...
        r.CoefficientOfVariation.ShouldBeGreaterThan(MaxCv); // ...but dispersion is too high
    }

    [Fact]
    public void FewerThanWindowSamples_NotSteady()
    {
        var r = Check(new double[] { 10, 10, 10 });
        r.ReachedSteadyState.ShouldBeFalse();
        r.Reason.ShouldContain("need");
    }

    [Fact]
    public void ColdStartSpikeOutsideWindow_DoesNotBlockSteadyState()
    {
        // A huge first sample shouldn't matter once it's outside the window.
        var r = Check(new double[] { 100000, 12, 12, 11, 12, 12, 11, 12 });
        r.ReachedSteadyState.ShouldBeTrue();
    }
}
