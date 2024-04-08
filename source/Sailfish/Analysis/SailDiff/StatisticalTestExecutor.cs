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
    private readonly IKolmogorovSmirnovTest kolmogorovSmirnovTestSailfish;
    private readonly IMannWhitneyWilcoxonTest mannWhitneyWilcoxonTestSailfish;
    private readonly ITTest ttest;
    private readonly ITwoSampleWilcoxonSignedRankTest twoSampWilcoxonSignedRankTestSailfish;

    public StatisticalTestExecutor(IMannWhitneyWilcoxonTest mannWhitneyWilcoxonTestSailfish,
        ITTest ttest,
        ITwoSampleWilcoxonSignedRankTest twoSampWilcoxonSignedRankTestSailfish,
        IKolmogorovSmirnovTest kolmogorovSmirnovTestSailfish)
    {
        this.kolmogorovSmirnovTestSailfish = kolmogorovSmirnovTestSailfish;
        this.mannWhitneyWilcoxonTestSailfish = mannWhitneyWilcoxonTestSailfish;
        this.ttest = ttest;
        this.twoSampWilcoxonSignedRankTestSailfish = twoSampWilcoxonSignedRankTestSailfish;
    }

    public TestResultWithOutlierAnalysis ExecuteStatisticalTest(
        double[] beforeData,
        double[] afterData,
        SailDiffSettings settings)
    {
        var testMap = new Dictionary<TestType, ITest>
        {
            { TestType.Test, ttest },
            { TestType.WilcoxonRankSumTest, mannWhitneyWilcoxonTestSailfish },
            { TestType.TwoSampleWilcoxonSignedRankTest, twoSampWilcoxonSignedRankTestSailfish },
            { TestType.KolmogorovSmirnovTest, kolmogorovSmirnovTestSailfish }
        };

        if (!testMap.TryGetValue(settings.TestType, out var value)) throw new SailfishException($"Test type {settings.TestType.ToString()} not supported");

        return value.ExecuteTest(beforeData, afterData, settings);
    }
}