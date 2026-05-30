using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

/// <summary>
/// Hand-derivable goldens for the two-sample Kolmogorov-Smirnov statistic. The K-S statistic
/// D is the supremum of |F1(x) − F2(x)| over all x, where F1, F2 are the empirical CDFs.
/// All values below are first-principles derivations from that definition; any failure
/// indicates either a wrong implementation or a wrong test.
/// </summary>
public class KolmogorovSmirnovGoldenTests
{
    [Fact]
    public void Identical_Samples_ProduceZeroDStatistic()
    {
        // F1 == F2 everywhere, so sup |F1 − F2| = 0. This is the cleanest test case: any
        // implementation that returns nonzero D for identical inputs has an off-by-one.
        var sample = new double[] { 1, 2, 3, 4, 5 };
        var test = TwoSampleKolmogorovSmirnovFactory.Create(sample, sample);

        test.Statistic.ShouldBe(0.0, tolerance: 1e-12);
    }

    [Fact]
    public void Identical_Samples_LargerN_ProduceZeroDStatistic()
    {
        var sample = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();
        var test = TwoSampleKolmogorovSmirnovFactory.Create(sample, sample);

        test.Statistic.ShouldBe(0.0, tolerance: 1e-12);
    }

    [Fact]
    public void FullyDisjoint_Samples_ProduceMaximalDStatistic()
    {
        // sample1 = {1, 2, 3, 4, 5}, sample2 = {6, 7, 8, 9, 10}. At x = 5, F1(5) = 1 and
        // F2(5) = 0, so |F1 − F2| = 1. That's the maximum the statistic can take.
        var s1 = new double[] { 1, 2, 3, 4, 5 };
        var s2 = new double[] { 6, 7, 8, 9, 10 };
        var test = TwoSampleKolmogorovSmirnovFactory.Create(s1, s2);

        test.Statistic.ShouldBe(1.0, tolerance: 1e-12);
    }

    [Fact]
    public void HalfOverlap_TwoSamples_ProducesHalfDStatistic()
    {
        // sample1 = {1, 2, 3, 4}, sample2 = {3, 4, 5, 6}. Walk the union:
        //   x=1: F1=0.25, F2=0,    |diff|=0.25
        //   x=2: F1=0.50, F2=0,    |diff|=0.50
        //   x=3: F1=0.75, F2=0.25, |diff|=0.50
        //   x=4: F1=1.00, F2=0.50, |diff|=0.50
        //   x=5: F1=1.00, F2=0.75, |diff|=0.25
        //   x=6: F1=1.00, F2=1.00, |diff|=0
        // sup |F1 − F2| = 0.5.
        var s1 = new double[] { 1, 2, 3, 4 };
        var s2 = new double[] { 3, 4, 5, 6 };
        var test = TwoSampleKolmogorovSmirnovFactory.Create(s1, s2);

        test.Statistic.ShouldBe(0.5, tolerance: 1e-12);
    }

    [Fact]
    public void SingleElementShift_ProducesPredictableD()
    {
        // sample1 = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}, sample2 = same but plus 1:
        // {2, 3, 4, 5, 6, 7, 8, 9, 10, 11}. F1 reaches 1/10 at x=1, F2 still 0 there. As we
        // walk up, F1 leads F2 by exactly one observation everywhere on the overlap.
        // Maximum gap = 1/10.
        var s1 = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
        var s2 = Enumerable.Range(2, 10).Select(i => (double)i).ToArray();
        var test = TwoSampleKolmogorovSmirnovFactory.Create(s1, s2);

        test.Statistic.ShouldBe(0.1, tolerance: 1e-12);
    }

    [Fact]
    public void PValue_BoundedInUnitInterval()
    {
        // Sanity: whatever the statistic is, the p-value reported by the test must be a
        // valid probability. Pre-fix this was sometimes outside [0,1] for identical samples
        // because the inflated D fed into the asymptotic distribution.
        var s1 = new double[] { 1, 2, 3, 4, 5 };
        var test = TwoSampleKolmogorovSmirnovFactory.Create(s1, s1);

        test.PValue.ShouldBeInRange(0.0, 1.0);
    }
}
