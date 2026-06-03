using System;
using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using Sailfish.Analysis.SailDiff.Statistics;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics;

/// <summary>
/// Regression coverage for <see cref="MultipleComparisons.LogRatioPValue"/>, the parametric
/// log-ratio test behind the method-comparison tables. The headline failure: a perfectly
/// separated, ~400× difference where one method had (near-)zero variance was labelled
/// "Similar" (not significant). Root cause: the p-value was computed as
/// <c>2·(1 − StudentT.CDF(t))</c>, which collapses to exactly 0 for large t — the tail
/// underflows the ULP of 1.0 — and a 0 p-value is dropped / BH-adjusts to q = 0, which the
/// cell labels treat as not significant.
/// </summary>
public class MultipleComparisonsLogRatioPValueTests
{
    // The reported numbers: WithPlus baseline ≈ 16.972 ms (real spread) vs WithStringBuilder
    // ≈ 0.039 ms with zero observed variance at measurement resolution (SE = 0).
    private const double BaselineMean = 16.972;
    private const double BaselineSe = 0.4; // ≈ stddev 1.2ms / sqrt(10)
    private const double FastMean = 0.039;
    private const double FastSe = 0.0; // zero observed variance
    private const int N = 10;

    [Fact]
    public void NaiveOneMinusCdfForm_CollapsesToExactlyZero_DocumentingTheBug()
    {
        // The pre-fix computation. Asserting it really is exactly 0 anchors the regression below
        // to the actual failure mode (a dropped p-value) rather than a hypothetical one.
        var seLog = BaselineSe / BaselineMean; // FastSe = 0 contributes nothing
        var t = Math.Abs(Math.Log(FastMean / BaselineMean)) / seLog;
        var naiveP = 2.0 * Math.Max(0.0, 1.0 - StudentT.CDF(0, 1, N - 1, t));

        naiveP.ShouldBe(0.0); // a maximally-significant comparison read as p = 0
    }

    [Fact]
    public void ExtremeSeparation_WithZeroVarianceGroup_IsStrictlyPositiveAndSignificant()
    {
        var p = MultipleComparisons.LogRatioPValue(BaselineMean, BaselineSe, N, FastMean, FastSe, N);

        double.IsNaN(p).ShouldBeFalse();
        p.ShouldBeGreaterThan(0.0); // never the dropped-as-0 value again
        p.ShouldBeLessThan(0.05); // and it is, correctly, highly significant
    }

    [Fact]
    public void ExtremeSeparation_SurvivesFdrAndIsLabelledSignificant()
    {
        // End-to-end: the value must stay positive through BH-FDR and read as significant, i.e.
        // the cell label is "Improved"/"Slower", never "Similar".
        const double alpha = 0.05;
        var p = MultipleComparisons.LogRatioPValue(BaselineMean, BaselineSe, N, FastMean, FastSe, N);

        var qMap = MultipleComparisons.BenjaminiHochbergAdjust(
            new Dictionary<(string A, string B), double> { [("WithPlus", "WithStringBuilder")] = p });
        var q = qMap[MultipleComparisons.NormalizePair("WithPlus", "WithStringBuilder")];

        q.ShouldBeGreaterThan(0.0);
        SailDiffSignificance.IsSignificantPositive(q, alpha).ShouldBeTrue();
    }

    [Fact]
    public void EqualMeans_AreNotSignificant()
    {
        var p = MultipleComparisons.LogRatioPValue(5.0, 0.1, N, 5.0, 0.1, N);
        p.ShouldBe(1.0, tolerance: 1e-9);
    }

    [Fact]
    public void BothSidesNoVariance_Abstains_RegardlessOfMeans()
    {
        // With no usable variance on either side there is nothing to run a variance-based test
        // against, so the helper abstains (NaN → "Similar", no q). This is deliberate and is
        // distinct from the reported one-sided case above, where the baseline supplies the
        // variance. Holds whether the means differ or not.
        double.IsNaN(MultipleComparisons.LogRatioPValue(10.0, 0.0, N, 1.0, 0.0, N)).ShouldBeTrue();
        double.IsNaN(MultipleComparisons.LogRatioPValue(5.0, 0.0, N, 5.0, 0.0, N)).ShouldBeTrue();
    }

    [Fact]
    public void NonPositiveMean_IsUndefined()
    {
        double.IsNaN(MultipleComparisons.LogRatioPValue(0.0, 0.1, N, 1.0, 0.1, N)).ShouldBeTrue();
        double.IsNaN(MultipleComparisons.LogRatioPValue(1.0, 0.1, N, -1.0, 0.1, N)).ShouldBeTrue();
    }
}
