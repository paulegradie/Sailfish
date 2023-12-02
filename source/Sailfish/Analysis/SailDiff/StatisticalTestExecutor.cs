using System.Collections.Generic;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.SailDiff;

public interface IStatisticalTestExecutor
{
    TestResultWithOutlierAnalysis ExecuteStatisticalTest(double[] beforeData, double[] afterData, SailDiffSettings settings);
}

public class StatisticalTestExecutor : IStatisticalTestExecutor
{
    private readonly IMannWhitneyWilcoxonTestSailfish mannWhitneyWilcoxonTestSailfish;
    private readonly ITwoSampleWilcoxonSignedRankTestSailfish twoSampWilcoxonSignedRankTestSailfish;
    private readonly IKolmogorovSmirnovTestSailfish kolmogorovSmirnovTestSailfish;
    private readonly ITTestSailfish ttest;

    public StatisticalTestExecutor(
        IMannWhitneyWilcoxonTestSailfish mannWhitneyWilcoxonTestSailfish,
        ITTestSailfish ttest,
        ITwoSampleWilcoxonSignedRankTestSailfish twoSampWilcoxonSignedRankTestSailfish,
        IKolmogorovSmirnovTestSailfish kolmogorovSmirnovTestSailfish)
    {
        this.mannWhitneyWilcoxonTestSailfish = mannWhitneyWilcoxonTestSailfish;
        this.ttest = ttest;
        this.twoSampWilcoxonSignedRankTestSailfish = twoSampWilcoxonSignedRankTestSailfish;
        this.kolmogorovSmirnovTestSailfish = kolmogorovSmirnovTestSailfish;
    }

    public TestResultWithOutlierAnalysis ExecuteStatisticalTest(
        double[] beforeData,
        double[] afterData,
        SailDiffSettings settings)
    {
        var testMap = new Dictionary<TestType, ITest>()
        {
            { TestType.TTest, ttest },
            { TestType.WilcoxonRankSumTest, mannWhitneyWilcoxonTestSailfish },
            { TestType.TwoSampleWilcoxonSignedRankTest, twoSampWilcoxonSignedRankTestSailfish },
            { TestType.KolmogorovSmirnovTest, kolmogorovSmirnovTestSailfish },
        };

        if (!testMap.ContainsKey(settings.TestType)) throw new SailfishException($"Test type {settings.TestType.ToString()} not supported");

        return testMap[settings.TestType].ExecuteTest(beforeData, afterData, settings);
    }
}