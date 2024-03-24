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

public class StatisticalTestExecutor(
    IMannWhitneyWilcoxonTest mannWhitneyWilcoxonTestSailfish,
    ITTest ttest,
    ITwoSampleWilcoxonSignedRankTest twoSampWilcoxonSignedRankTestSailfish,
    IKolmogorovSmirnovTest kolmogorovSmirnovTestSailfish) : IStatisticalTestExecutor
{
    private readonly IKolmogorovSmirnovTest kolmogorovSmirnovTestSailfish = kolmogorovSmirnovTestSailfish;
    private readonly IMannWhitneyWilcoxonTest mannWhitneyWilcoxonTestSailfish = mannWhitneyWilcoxonTestSailfish;
    private readonly ITTest ttest = ttest;
    private readonly ITwoSampleWilcoxonSignedRankTest twoSampWilcoxonSignedRankTestSailfish = twoSampWilcoxonSignedRankTestSailfish;

    public TestResultWithOutlierAnalysis ExecuteStatisticalTest(
        double[] beforeData,
        double[] afterData,
        SailDiffSettings settings)
    {
        var testMap = new Dictionary<TestType, ITest>
        {
            { TestType.TTest, ttest },
            { TestType.WilcoxonRankSumTest, mannWhitneyWilcoxonTestSailfish },
            { TestType.TwoSampleWilcoxonSignedRankTest, twoSampWilcoxonSignedRankTestSailfish },
            { TestType.KolmogorovSmirnovTest, kolmogorovSmirnovTestSailfish }
        };

        if (!testMap.ContainsKey(settings.TestType)) throw new SailfishException($"Test type {settings.TestType.ToString()} not supported");

        return testMap[settings.TestType].ExecuteTest(beforeData, afterData, settings);
    }
}