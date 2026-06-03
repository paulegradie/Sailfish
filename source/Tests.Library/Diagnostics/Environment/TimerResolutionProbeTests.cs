using Sailfish.Diagnostics.Environment;
using Shouldly;
using Xunit;

namespace Tests.Library.Diagnostics.Environment;

/// <summary>
/// Deterministic coverage for the effective-resolution probe. The live sampler
/// (<c>MeasureEffectiveResolutionNs</c>) is hardware-dependent, so the testable seam is the pure
/// reducer <see cref="EnvironmentHealthChecker.EffectiveResolutionNsFromTickDeltas"/> fed synthetic
/// tick deltas.
/// </summary>
public class TimerResolutionProbeTests
{
    private const long GHz = 1_000_000_000; // Stopwatch.Frequency on macOS/Linux: 1 tick == 1 ns
    private const long TenMHz = 10_000_000;  // typical Windows QPC: 1 tick == 100 ns

    [Fact]
    public void EffectiveResolution_takes_the_smallest_nonzero_delta()
    {
        // Apple-Silicon-shaped sample: Frequency advertises 1 ns/tick, but the counter advances in
        // ~42-tick jumps and sub-tick reads show 0. The observed quantum is 42 ns, not the reported 1 ns.
        var deltas = new long[] { 0, 0, 42, 0, 42, 84, 0, 42 };
        EnvironmentHealthChecker.EffectiveResolutionNsFromTickDeltas(deltas, GHz).ShouldBe(42.0, 1e-9);
    }

    [Fact]
    public void EffectiveResolution_ignores_upward_outliers_from_scheduler_hiccups()
    {
        // A context switch inflates one delta to a million ticks; taking the MIN must ignore it.
        var deltas = new long[] { 41, 42, 1_000_000, 41, 42 };
        EnvironmentHealthChecker.EffectiveResolutionNsFromTickDeltas(deltas, GHz).ShouldBe(41.0, 1e-9);
    }

    [Fact]
    public void EffectiveResolution_scales_by_frequency()
    {
        // On a 10 MHz counter each tick is 100 ns, so a 1-tick min delta => 100 ns effective resolution.
        var deltas = new long[] { 1, 2, 3, 1 };
        EnvironmentHealthChecker.EffectiveResolutionNsFromTickDeltas(deltas, TenMHz).ShouldBe(100.0, 1e-9);
    }

    [Fact]
    public void EffectiveResolution_equals_reported_when_counter_advances_every_tick()
    {
        // A genuinely fine timer advances one tick at a time => effective == reported (1 ns at 1 GHz).
        var deltas = new long[] { 1, 1, 1, 1 };
        EnvironmentHealthChecker.EffectiveResolutionNsFromTickDeltas(deltas, GHz).ShouldBe(1.0, 1e-9);
    }

    [Fact]
    public void EffectiveResolution_is_NaN_when_no_advance_is_observed()
    {
        // All sub-tick reads (or no samples at all) => caller falls back to the reported value.
        double.IsNaN(EnvironmentHealthChecker.EffectiveResolutionNsFromTickDeltas(new long[] { 0, 0, 0 }, GHz))
            .ShouldBeTrue();
        double.IsNaN(EnvironmentHealthChecker.EffectiveResolutionNsFromTickDeltas(new long[0], GHz))
            .ShouldBeTrue();
    }
}
