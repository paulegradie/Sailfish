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

        var sample1 = preprocessor.PreprocessWithDownSample(before, settings.UseInnerQuartile, downSampleSize, minSampleSize);
        var sample2 = preprocessor.PreprocessWithDownSample(after, settings.UseInnerQuartile, downSampleSize, minSampleSize);

        var test = new TwoSampleWilcoxonSignedRankTest(
            sample1,
            sample2,
            TwoSampleHypothesis.ValuesAreDifferent);

        var meanBefore = Math.Round(before.Mean(), sigDig);
        var meanAfter = Math.Round(after.Mean(), sigDig);

        var medianBefore = Math.Round(before.Median(), sigDig);
        var medianAfter = Math.Round(after.Median(), sigDig);

        var testStatistic = Math.Round(test.Statistic, sigDig);
        var pval = test.PValue;
        var isSignificant = pval <= settings.Alpha;

        var changeDirection = medianAfter > medianBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;
        var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

        var additionalResults = new Dictionary<string, object>();

        return new TestResults(
            meanBefore,
            meanAfter,
            medianBefore,
            medianAfter,
            testStatistic,
            pval,
            description,
            before.Length,
            after.Length,
            before,
            after,
            additionalResults);
    }
}