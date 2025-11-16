using System.Collections.Generic;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
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

    public StatisticalTestExecutor(IMannWhitneyWilcoxonTest mannWhitneyWilcoxonTestSailfish,
        ITTest ttest,
        ITwoSampleWilcoxonSignedRankTest twoSampWilcoxonSignedRankTestSailfish,
        IKolmogorovSmirnovTest kolmogorovSmirnovTestSailfish)
    {
        _kolmogorovSmirnovTestSailfish = kolmogorovSmirnovTestSailfish;
        _mannWhitneyWilcoxonTestSailfish = mannWhitneyWilcoxonTestSailfish;
        _ttest = ttest;
        _twoSampWilcoxonSignedRankTestSailfish = twoSampWilcoxonSignedRankTestSailfish;
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
            { TestType.KolmogorovSmirnovTest, _kolmogorovSmirnovTestSailfish }
        };

        if (!testMap.TryGetValue(settings.TestType, out var value)) throw new SailfishException($"Test type {settings.TestType.ToString()} not supported");

        return value.ExecuteTest(beforeData, afterData, settings);
    }
}