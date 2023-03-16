using System;
using System.Collections.Generic;
using Accord.Statistics;
using Accord.Statistics.Testing;
using Sailfish.Analysis;
using Sailfish.Contracts;

namespace Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;

public class TwoSampleWilcoxonSignedRankTestSailfish : ITwoSampleWilcoxonSignedRankTestSailfish
{
    private readonly ITestPreprocessor preprocessor;

    public TwoSampleWilcoxonSignedRankTestSailfish(ITestPreprocessor preprocessor)
    {
        this.preprocessor = preprocessor;
    }

    public TestResults ExecuteTest(double[] before, double[] after, TestSettings settings)
    {
        var sigDig = settings.Round;

        var downSampleSize = Math.Min(before.Length, after.Length);
        var minSampleSize = Math.Min(downSampleSize, 3);

        var sample1 = preprocessor.PreprocessWithDownSample(before, settings.UseInnerQuartile, true, downSampleSize, minSampleSize);
        var sample2 = preprocessor.PreprocessWithDownSample(after, settings.UseInnerQuartile, true, downSampleSize, minSampleSize);

        var test = new TwoSampleWilcoxonSignedRankTest(
            sample1,
            sample2,
            TwoSampleHypothesis.ValuesAreDifferent);

        var meanBefore = Math.Round(sample1.Mean(), sigDig);
        var meanAfter = Math.Round(sample2.Mean(), sigDig);

        var medianBefore = Math.Round(sample1.Median(), sigDig);
        var medianAfter = Math.Round(sample2.Median(), sigDig);

        var testStatistic = test.Statistic;
        var isSignificant = test.PValue <= settings.Alpha;
        var pVal = Math.Round(test.PValue, TestConstants.PValueSigDig);
        var changeDirection = medianAfter > medianBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;

        var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

        var additionalResults = new Dictionary<string, object>();

        return new TestResults(
            meanBefore,
            meanAfter,
            medianBefore,
            medianAfter,
            testStatistic,
            pVal,
            description,
            before.Length,
            after.Length,
            before,
            after,
            additionalResults);
    }
}