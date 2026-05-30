using System;
using Sailfish.Analysis.SailDiff.Statistics;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// Hand-derivable goldens for the MDE / sample-size utility.
/// </summary>
/// <remarks>
/// Reference values come from the closed-form expression
/// <c>MDE = (z_{1−α/2} + z_{1−β}) · sqrt(var1/n1 + var2/n2)</c>
/// with <c>z_{0.975} = 1.95996...</c> and <c>z_{0.80} = 0.84162...</c>.
/// </remarks>
public class MinimumDetectableEffectTests
{
    [Fact]
    public void Absolute_DefaultPower_MatchesHandFormula()
    {
        // var1 = var2 = 4, n1 = n2 = 100, α = 0.05, power = 0.80.
        // SE = sqrt(4/100 + 4/100) = sqrt(0.08) = 0.28284...
        // MDE = (1.95996 + 0.84162) × 0.28284 ≈ 0.79248
        var result = MinimumDetectableEffect.Absolute(
            variance1: 4, n1: 100,
            variance2: 4, n2: 100,
            alpha: 0.05);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe(0.7925, tolerance: 1e-3);
    }

    [Fact]
    public void Absolute_TighterAlpha_RequiresLargerEffect()
    {
        // Same data, tighter α should produce a larger MDE because z_{1−α/2} grows.
        var loose = MinimumDetectableEffect.Absolute(4, 100, 4, 100, alpha: 0.05)!.Value;
        var tight = MinimumDetectableEffect.Absolute(4, 100, 4, 100, alpha: 0.01)!.Value;

        tight.ShouldBeGreaterThan(loose);
    }

    [Fact]
    public void Absolute_HigherPower_RequiresLargerEffect()
    {
        // More demanding power target (β smaller) requires a larger detectable effect.
        var low = MinimumDetectableEffect.Absolute(4, 100, 4, 100, alpha: 0.05, power: 0.80)!.Value;
        var high = MinimumDetectableEffect.Absolute(4, 100, 4, 100, alpha: 0.05, power: 0.95)!.Value;

        high.ShouldBeGreaterThan(low);
    }

    [Fact]
    public void Absolute_LargerN_ReducesDetectableEffect()
    {
        // More samples → tighter SE → smaller MDE.
        var small = MinimumDetectableEffect.Absolute(4, 30, 4, 30, alpha: 0.05)!.Value;
        var large = MinimumDetectableEffect.Absolute(4, 300, 4, 300, alpha: 0.05)!.Value;

        large.ShouldBeLessThan(small);
        // sqrt(N) scaling: 10× the samples should shrink MDE by sqrt(10) ≈ 3.16×.
        (small / large).ShouldBe(Math.Sqrt(10.0), tolerance: 0.05);
    }

    [Fact]
    public void Absolute_NullForTooFewSamples()
    {
        MinimumDetectableEffect.Absolute(1, 1, 1, 5, 0.05).ShouldBeNull();
        MinimumDetectableEffect.Absolute(1, 5, 1, 1, 0.05).ShouldBeNull();
    }

    [Fact]
    public void Absolute_NullForInvalidAlphaOrPower()
    {
        MinimumDetectableEffect.Absolute(1, 10, 1, 10, alpha: -0.1).ShouldBeNull();
        MinimumDetectableEffect.Absolute(1, 10, 1, 10, alpha: 1.5).ShouldBeNull();
        MinimumDetectableEffect.Absolute(1, 10, 1, 10, alpha: 0.05, power: 0).ShouldBeNull();
        MinimumDetectableEffect.Absolute(1, 10, 1, 10, alpha: 0.05, power: 1.0).ShouldBeNull();
    }

    [Fact]
    public void Absolute_NullForZeroVariance()
    {
        // SE collapses to 0 — the MDE expression returns 0 mathematically, which we surface
        // as null since "any non-zero difference is detectable" isn't a useful answer.
        MinimumDetectableEffect.Absolute(0, 10, 0, 10, alpha: 0.05).ShouldBeNull();
    }

    [Fact]
    public void RelativePercent_ConvertsToShareOfPooledMean()
    {
        // mean1 = mean2 = 100, var1 = var2 = 4, n1 = n2 = 100, α = 0.05, power = 0.80
        // Pooled mean = 100. Absolute MDE ≈ 0.7925.
        // Relative% = 0.7925 / 100 × 100 = 0.7925%.
        var result = MinimumDetectableEffect.RelativePercent(
            mean1: 100, variance1: 4, n1: 100,
            mean2: 100, variance2: 4, n2: 100,
            alpha: 0.05);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe(0.7925, tolerance: 1e-3);
    }

    [Fact]
    public void RelativePercent_NullForNonPositiveMean()
    {
        // Zero / negative pooled mean ⇒ relative MDE undefined.
        MinimumDetectableEffect.RelativePercent(0, 4, 100, 0, 4, 100, 0.05).ShouldBeNull();
        MinimumDetectableEffect.RelativePercent(-10, 4, 100, -10, 4, 100, 0.05).ShouldBeNull();
    }

    [Fact]
    public void SampleSizePerGroup_InvertsTheMdeFormula()
    {
        // Round-trip: given a target absolute MDE of 0.7925 with var = 4 and α = 0.05,
        // power = 0.80, the formula should recover N ≈ 100. Round up gives 100.
        var n = MinimumDetectableEffect.SampleSizePerGroup(0.7925, variance: 4, alpha: 0.05);
        n.ShouldNotBeNull();
        n!.Value.ShouldBeInRange(99, 101);
    }

    [Fact]
    public void SampleSizePerGroup_SmallerEffect_RequiresLargerN()
    {
        var nBig = MinimumDetectableEffect.SampleSizePerGroup(1.0, 4, 0.05)!.Value;
        var nSmall = MinimumDetectableEffect.SampleSizePerGroup(0.1, 4, 0.05)!.Value;

        // Effect 10× smaller ⇒ N 100× larger.
        nSmall.ShouldBeGreaterThan(nBig * 90);
    }

    [Fact]
    public void SampleSizePerGroup_ZeroEffectIsUndefined()
    {
        MinimumDetectableEffect.SampleSizePerGroup(0, 4, 0.05).ShouldBeNull();
        MinimumDetectableEffect.SampleSizePerGroup(-1, 4, 0.05).ShouldBeNull();
    }

    [Fact]
    public void SampleSizePerGroup_ZeroVarianceCollapsesToTwo()
    {
        // No noise → any non-zero effect is detectable at N=2 (minimum for the t-test).
        MinimumDetectableEffect.SampleSizePerGroup(0.5, variance: 0, alpha: 0.05).ShouldBe(2);
    }
}

