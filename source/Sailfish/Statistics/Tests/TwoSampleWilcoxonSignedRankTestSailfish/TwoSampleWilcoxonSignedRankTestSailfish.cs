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

        var sample1 = preprocessor.Preprocess(before, settings.UseInnerQuartile);
        var sample2 = preprocessor.Preprocess(after, settings.UseInnerQuartile);

        var test = new TwoSampleWilcoxonSignedRankTest(
            sample1,
            sample2,
            TwoSampleHypothesis.ValuesAreDifferent);

        var meanBefore = Math.Round(before.Mean(), sigDig);
        var meanAfter = Math.Round(after.Mean(), sigDig);

        var medianBefore = Math.Round(before.Median(), sigDig);
        var medianAfter = Math.Round(after.Median(), sigDig);

        var testStatistic = test.Statistic;
        var isSignificant = test.PValue <= settings.Alpha;
        var pVal = Math.Round(test.PValue, TestConstants.PValueSigDig);
        var changeDirection = medianAfter > medianBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;

        var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;

        var additionalResults = new Dictionary<string, double>();

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
            additionalResults);
    }
}