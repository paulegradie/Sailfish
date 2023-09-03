using Sailfish.Analysis.SailDiff;
using Sailfish.MathOps;
using Sailfish.Statistics.Tests;
using Sailfish.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Statistics.Tests.TTestSailfish;
using Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Shouldly;
using Xunit;

namespace Test.Statistics;

public class StatisticalTestFixture
{
    [Fact]
    public void WhenStdDevIsZeroMannWhitneyWilcoxonTestSailfishDoesNotThrow()
    {
        var test = new MannWhitneyWilcoxonTestSailfish(new TestPreprocessor(new OutlierDetector()));

        var before = new[] { 0.0, 0, 0, 0, 0 };
        var after = new[] { 0.0, 0, 0, 0, 0 };

        Should.NotThrow(() => test.ExecuteTest(before, after, new TestSettings(0.001, 2)));
    }

    [Fact]
    public void WhenStdDevIsZeroTwoSampleWilcoxonSignedRankTestSailfishDoesNotThrow()
    {
        var test = new TwoSampleWilcoxonSignedRankTestSailfish(new TestPreprocessor(new OutlierDetector()));

        var before = new[] { 0.0, 0, 0, 0, 0 };
        var after = new[] { 0.0, 0, 0, 0, 0 };

        Should.NotThrow(() => test.ExecuteTest(before, after, new TestSettings(0.001, 2)));
    }

    [Fact]
    public void WhenStdDevIsZeroTestSailfishDoesNotThrow()
    {
        var test = new TTestSailfish(new TestPreprocessor(new OutlierDetector()));

        var before = new[] { 0.0, 0, 0, 0, 0 };
        var after = new[] { 0.0, 0, 0, 0, 0 };

        Should.NotThrow(() => test.ExecuteTest(before, after, new TestSettings(0.001, 2)));
    }
}