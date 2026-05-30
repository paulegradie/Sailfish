using Sailfish.Analysis.SailDiff.Statistics.StatsCore;
using System;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class MannWhitneyDistributionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 };

        // Act
        var distribution = new MannWhitneyDistribution(ranks, 3, 3);

        // Assert
        distribution.ShouldNotBeNull();
        distribution.NumberOfSamples1.ShouldBe(3);
        distribution.NumberOfSamples2.ShouldBe(3);
    }

    [Fact]
    public void Constructor_WithSmallSamples_ShouldUseExactMethod()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0 };

        // Act
        var distribution = new MannWhitneyDistribution(ranks, 2, 2);

        // Assert
        distribution.Exact.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithLargeSamples_ShouldUseApproximation()
    {
        // Arrange — N=70,70 is well beyond the DP cap of 50, forcing the normal approximation.
        var ranks = Enumerable.Range(1, 140).Select(x => (double)x).ToArray();

        // Act
        var distribution = new MannWhitneyDistribution(ranks, 70, 70);

        // Assert
        distribution.Exact.ShouldBeFalse();
    }

    [Fact]
    public void NumberOfSamples1_ShouldReturnConstructorValue()
    {
        // Arrange & Act
        var distribution = new MannWhitneyDistribution([1.0, 2.0, 3.0, 4.0], 2, 2);

        // Assert
        distribution.NumberOfSamples1.ShouldBe(2);
    }

    [Fact]
    public void NumberOfSamples2_ShouldReturnConstructorValue()
    {
        // Arrange & Act
        var distribution = new MannWhitneyDistribution([1.0, 2.0, 3.0, 4.0], 2, 2);

        // Assert
        distribution.NumberOfSamples2.ShouldBe(2);
    }

    [Fact]
    public void Correction_ShouldDefaultToMidpoint()
    {
        // Arrange & Act
        var distribution = new MannWhitneyDistribution([1.0, 2.0, 3.0, 4.0], 2, 2);

        // Assert
        distribution.Correction.ShouldBe(ContinuityCorrection.Midpoint);
    }

    [Fact]
    public void Mean_ShouldCalculateCorrectly()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 };

        // Act
        var distribution = new MannWhitneyDistribution(ranks, 3, 3);

        // Assert - Mean = n1*n2/2 = 3*3/2 = 4.5
        distribution.Mean.ShouldBe(4.5);
    }

    [Fact]
    public void Support_ShouldReturnInfiniteRange()
    {
        // Arrange
        var distribution = new MannWhitneyDistribution([1.0, 2.0, 3.0, 4.0], 2, 2);

        // Act
        var support = distribution.Support;

        // Assert
        support.Min.ShouldBe(double.NegativeInfinity);
        support.Max.ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void DistributionFunction_WithExactMethod_ShouldWorkCorrectly()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0 };
        var distribution = new MannWhitneyDistribution(ranks, 2, 2);

        // Act
        var result = distribution.DistributionFunction(distribution.Mean);

        // Assert - Should return a value between 0 and 1
        result.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void DistributionFunction_WithApproximation_ShouldWorkCorrectly()
    {
        // Arrange
        var ranks = Enumerable.Range(1, 140).Select(x => (double)x).ToArray();
        var distribution = new MannWhitneyDistribution(ranks, 70, 70);

        // Act
        var result = distribution.DistributionFunction(distribution.Mean);

        // Assert - At mean, CDF should be approximately 0.5
        result.ShouldBe(0.5, 0.1);
    }

    [Fact]
    public void ComplementaryDistributionFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new MannWhitneyDistribution([1.0, 2.0, 3.0, 4.0], 2, 2);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.ComplementaryDistributionFunction(double.NaN));
        exception.ParamName.ShouldBe("x");
    }

    [Fact]
    public void ProbabilityDensityFunction_WithExactMethod_ShouldReturnPositiveValue()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0 };
        var distribution = new MannWhitneyDistribution(ranks, 2, 2);

        // Act
        var result = distribution.ProbabilityDensityFunction(2.0);

        // Assert
        result.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ProbabilityDensityFunction_WithApproximation_ShouldReturnPositiveValue()
    {
        // Arrange
        var ranks = Enumerable.Range(1, 140).Select(x => (double)x).ToArray();
        var distribution = new MannWhitneyDistribution(ranks, 70, 70);

        // Act
        var result = distribution.ProbabilityDensityFunction(distribution.Mean);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void InverseDistributionFunction_WithExactMethod_ShouldWorkCorrectly()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0 };
        var distribution = new MannWhitneyDistribution(ranks, 2, 2);

        // Act
        var result = distribution.InverseDistributionFunction(0.5);

        // Assert - Should return a finite value
        double.IsFinite(result).ShouldBeTrue();
    }

    [Fact]
    public void InverseDistributionFunction_WithApproximation_ShouldReturnMeanAtHalf()
    {
        // Arrange
        var ranks = Enumerable.Range(1, 140).Select(x => (double)x).ToArray();
        var distribution = new MannWhitneyDistribution(ranks, 70, 70);

        // Act
        var result = distribution.InverseDistributionFunction(0.5);

        // Assert - At p=0.5, should return approximately the mean
        result.ShouldBe(distribution.Mean, 1.0);
    }

    [Fact]
    public void ToString_WithDefaultFormat_ShouldReturnFormattedString()
    {
        // Arrange
        var distribution = new MannWhitneyDistribution([1.0, 2.0, 3.0, 4.0], 2, 2);

        // Act
        var result = distribution.ToString();

        // Assert
        result.ShouldContain("MannWhitney");
        result.ShouldContain("n1");
        result.ShouldContain("n2");
        result.ShouldContain("2");
    }

    [Fact]
    public void ToString_WithCustomFormat_ShouldFormatNumbers()
    {
        // Arrange
        var distribution = new MannWhitneyDistribution([1.0, 2.0, 3.0, 4.0], 2, 2);

        // Act
        var result = distribution.ToString("F0", null);

        // Assert
        result.ShouldContain("2");
    }

    [Fact]
    public void Constructor_WithSingleElementRanks_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var ranks = new[] { 1.0 };

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new MannWhitneyDistribution(ranks, 1, 0));
    }

    [Fact]
    public void Constructor_WithDifferentSampleSizes_ShouldWorkCorrectly()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var distribution = new MannWhitneyDistribution(ranks, 2, 3);

        // Assert
        distribution.NumberOfSamples1.ShouldBe(2);
        distribution.NumberOfSamples2.ShouldBe(3);
        distribution.Mean.ShouldBe(3.0); // 2*3/2 = 3
    }

    [Fact]
    public void DistributionFunction_WithNaN_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new MannWhitneyDistribution([1.0, 2.0, 3.0, 4.0], 2, 2);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.DistributionFunction(double.NaN));
        exception.ParamName.ShouldBe("x");
    }

    [Fact]
    public void InverseDistributionFunction_WithInvalidProbability_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var distribution = new MannWhitneyDistribution([1.0, 2.0, 3.0, 4.0], 2, 2);

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            distribution.InverseDistributionFunction(1.5));
        exception.ParamName.ShouldBe("p");
    }

    [Fact]
    public void InverseDistributionFunction_WithZero_ShouldReturnNegativeInfinity()
    {
        // Arrange
        var ranks = Enumerable.Range(1, 140).Select(x => (double)x).ToArray();
        var distribution = new MannWhitneyDistribution(ranks, 70, 70);

        // Act
        var result = distribution.InverseDistributionFunction(0.0);

        // Assert
        result.ShouldBe(double.NegativeInfinity);
    }

    [Fact]
    public void InverseDistributionFunction_WithOne_ShouldReturnPositiveInfinity()
    {
        // Arrange
        var ranks = Enumerable.Range(1, 140).Select(x => (double)x).ToArray();
        var distribution = new MannWhitneyDistribution(ranks, 70, 70);

        // Act
        var result = distribution.InverseDistributionFunction(1.0);

        // Assert
        result.ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void Constructor_WithTiedRanks_ShouldHandleCorrectly()
    {
        // Arrange
        var ranks = new[] { 1.0, 2.5, 2.5, 4.0, 5.0, 6.0 };

        // Act
        var distribution = new MannWhitneyDistribution(ranks, 3, 3);

        // Assert - Should create distribution with tie correction
        distribution.ShouldNotBeNull();
        distribution.Mean.ShouldBe(4.5);
    }

    // ─── Regression: asymmetric n1 vs n2 returns correct CCDF ─────────────────────────────
    //
    // Pre-fix, MannWhitneyDistribution.InnerComplementaryDistributionFunction branched on
    // n1 vs n2 and returned the CDF when n1 > n2 — a leftover from the pre-DP era. The
    // wrapper's two-sided p-value formula 2·min(F(U), F_c(U)) then collapsed for n1 > n2
    // to 2·F(U) with U = U2 = max, returning p = 1 even for maximally-separated data.
    //
    // These tests pin both sides of the fix: the CCDF is correct in both n1 < n2 and
    // n1 > n2 orientations, the CDF + CCDF satisfy the documented discrete identity
    // F(u) + F_c(u) = 1 + P(U = u), and the orientation symmetry P(U1 ≤ u) = P(U2 ≥ n1·n2 − u)
    // holds across both sample-size relations.

    [Fact]
    public void Ccdf_NLargerThanM_ReturnsCcdfNotCdf()
    {
        // 5 vs 4 — the "n1 > n2" path that the asymmetric branch corrupted. Untied ranks so
        // the exact DP path activates.
        var ranks = Enumerable.Range(1, 9).Select(x => (double)x).ToArray();
        var distribution = new MannWhitneyDistribution(ranks, 5, 4);

        // U support is {0, ..., 20}; mid-support u = 10 should have F + Fc = 1 + PMF(10).
        var f = distribution.DistributionFunction(10);
        var fc = distribution.ComplementaryDistributionFunction(10);
        var pmfAt10 = distribution.ProbabilityDensityFunction(10);

        (f + fc).ShouldBe(1.0 + pmfAt10, tolerance: 1e-9);

        // At the upper boundary U = 20, P(U ≥ 20) = PMF(20) > 0, so Fc(20) is strictly
        // positive — not F(20) = 1.0, which the pre-fix code would have returned.
        var fcMax = distribution.ComplementaryDistributionFunction(20);
        fcMax.ShouldBeGreaterThan(0);
        fcMax.ShouldBeLessThan(0.1, customMessage: "tail PMF at U=max should be small but non-zero");
    }

    [Fact]
    public void Ccdf_IsConsistentAcrossSampleSizeOrientations()
    {
        // The U-statistic null distribution is symmetric: passing (n1, n2) or (n2, n1)
        // must yield the same PMF at every support point and the same CCDF at every query.
        var ranks = Enumerable.Range(1, 9).Select(x => (double)x).ToArray();
        var d54 = new MannWhitneyDistribution(ranks, 5, 4);  // n1 > n2 — the pre-fix bug path
        var d45 = new MannWhitneyDistribution(ranks, 4, 5);  // n1 < n2 — pre-fix correct path

        for (var u = 0; u <= 20; u++)
        {
            d54.ComplementaryDistributionFunction(u).ShouldBe(
                d45.ComplementaryDistributionFunction(u),
                tolerance: 1e-12,
                customMessage: $"CCDF at u={u} should not depend on n1 vs n2 orientation");
        }
    }

    [Fact]
    public void TwoSidedPValue_MaxSeparation_NLargerThanM_RejectsCorrectly()
    {
        // The agent's regression example: before=[1..5], after=[6..9]. Ranks are 1..9.
        // Rank1 = [1,2,3,4,5], Rank2 = [6,7,8,9]. U1 = 0, U2 = 20 = n1·n2 (max separation).
        // Pre-fix wrapper computed p ≈ min(2·F(U2), 1) = min(2·1, 1) = 1.
        // Post-fix: p ≈ 2·min(F(20), F_c(20)) where F_c(20) = PMF(20) ≈ 1/126 ≈ 0.0079.
        // So p ≈ 2·0.0079 ≈ 0.016 — small enough to reject at α = 0.05.
        var ranks = Enumerable.Range(1, 9).Select(x => (double)x).ToArray();
        var distribution = new MannWhitneyDistribution(ranks, 5, 4);

        // Mirror the wrapper's two-sided formula directly so this test is robust to wrapper
        // refactors and isolates the distribution-level fix.
        const double u = 20.0;
        var twoSidedP = Math.Min(2.0 * Math.Min(distribution.DistributionFunction(u),
                                                distribution.ComplementaryDistributionFunction(u)), 1.0);

        twoSidedP.ShouldBeLessThan(0.05,
            customMessage: "Maximally-separated 5-vs-4 sample must yield p < 0.05; the pre-fix branch returned p = 1.");
        twoSidedP.ShouldBeGreaterThan(0.0,
            customMessage: "p-value must be strictly positive (smallest possible for n1=5, n2=4 is 2/C(9,4) = 2/126 ≈ 0.016).");
    }
}

