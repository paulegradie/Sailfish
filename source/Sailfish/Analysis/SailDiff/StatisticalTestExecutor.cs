using System.Collections.Generic;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.PermutationTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.SailDiff;

public interface IStatisticalTestExecutor
{
    TestResultWithOutlierAnalysis ExecuteStatisticalTest(double[] beforeData, double[] afterData, SailDiffSettings settings);
}

public class StatisticalTestExecutor : IStatisticalTestExecutor
{
    private readonly IKolmogorovSmirnovTest _kolmogorovSmirnovTestSailfish;
    private readonly IMannWhitneyWilcoxonTest _mannWhitneyWilcoxonTestSailfish;
    private readonly ITTest _ttest;
    private readonly ITwoSampleWilcoxonSignedRankTest _twoSampWilcoxonSignedRankTestSailfish;
    private readonly IPermutationTest _permutationTest;

    public StatisticalTestExecutor(IMannWhitneyWilcoxonTest mannWhitneyWilcoxonTestSailfish,
        ITTest ttest,
        ITwoSampleWilcoxonSignedRankTest twoSampWilcoxonSignedRankTestSailfish,
        IKolmogorovSmirnovTest kolmogorovSmirnovTestSailfish,
        IPermutationTest permutationTest)
    {
        _kolmogorovSmirnovTestSailfish = kolmogorovSmirnovTestSailfish;
        _mannWhitneyWilcoxonTestSailfish = mannWhitneyWilcoxonTestSailfish;
        _ttest = ttest;
        _twoSampWilcoxonSignedRankTestSailfish = twoSampWilcoxonSignedRankTestSailfish;
        _permutationTest = permutationTest;
    }

    public TestResultWithOutlierAnalysis ExecuteStatisticalTest(
        double[] beforeData,
        double[] afterData,
        SailDiffSettings settings)
    {
        var testMap = new Dictionary<TestType, ITest>
        {
            { TestType.Test, _ttest },
            { TestType.WilcoxonRankSumTest, _mannWhitneyWilcoxonTestSailfish },
            { TestType.TwoSampleWilcoxonSignedRankTest, _twoSampWilcoxonSignedRankTestSailfish },
            { TestType.KolmogorovSmirnovTest, _kolmogorovSmirnovTestSailfish },
            { TestType.PermutationTest, _permutationTest }
        };

        if (!testMap.TryGetValue(settings.TestType, out var value)) throw new SailfishException($"Test type {settings.TestType.ToString()} not supported");

        var result = value.ExecuteTest(beforeData, afterData, settings);
        AttachEquivalence(result, settings);
        return result;
    }

    /// <summary>
    /// Runs the opt-in TOST equivalence check as a supplement to whichever significance test the
    /// user selected. Computed here — the single chokepoint every test type passes through — on the
    /// processed samples the main test actually consumed (RawDataBefore/After carry the
    /// post-outlier-removal data), so the equivalence statement and the significance verdict always
    /// describe the same observations.
    /// </summary>
    private static void AttachEquivalence(TestResultWithOutlierAnalysis result, SailDiffSettings settings)
    {
        if (settings.EquivalenceMarginPercent is not double marginPercent) return;

        var stat = result?.StatisticalTestResult;
        if (stat is null || stat.Failed) return;
        if (stat.RawDataBefore is null || stat.RawDataAfter is null) return;

        stat.Equivalence = EquivalenceTest.LogScaleTost(stat.RawDataBefore, stat.RawDataAfter, marginPercent, settings.Alpha);
    }
}