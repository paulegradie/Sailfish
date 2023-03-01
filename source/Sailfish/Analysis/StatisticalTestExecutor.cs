using Sailfish.Exceptions;
using Sailfish.Statistics.Tests;
using Sailfish.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Statistics.Tests.TTestSailfish;
using Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;

namespace Sailfish.Analysis;

public class StatisticalTestExecutor : IStatisticalTestExecutor
{
    private readonly IMannWhitneyWilcoxonTestSailfish mannWhitneyWilcoxonTestSailfish;
    private readonly ITwoSampleWilcoxonSignedRankTestSailfish twoSampWilcoxonSignedRankTestSailfish;
    private readonly ITTestSailfish ttest;

    public StatisticalTestExecutor(
        IMannWhitneyWilcoxonTestSailfish mannWhitneyWilcoxonTestSailfish,
        ITTestSailfish ttest,
        ITwoSampleWilcoxonSignedRankTestSailfish twoSampWilcoxonSignedRankTestSailfish)
    {
        this.mannWhitneyWilcoxonTestSailfish = mannWhitneyWilcoxonTestSailfish;
        this.ttest = ttest;
        this.twoSampWilcoxonSignedRankTestSailfish = twoSampWilcoxonSignedRankTestSailfish;
    }

    public TestResults ExecuteStatisticalTest(
        double[] beforeData,
        double[] afterData,
        TestSettings settings)
    {
        return settings.TestType switch
        {
            TestType.TTest => ttest.ExecuteTest(beforeData, afterData, settings),
            TestType.WilcoxonRankSumTest => mannWhitneyWilcoxonTestSailfish.ExecuteTest(beforeData, afterData, settings),
            TestType.TwoSampleWilcoxonSignedRankTest => twoSampWilcoxonSignedRankTestSailfish.ExecuteTest(beforeData, afterData, settings),
            _ => throw new SailfishException($"Test type {settings.TestType.ToString()} not supported")
        };
    }
}