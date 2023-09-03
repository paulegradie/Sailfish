using System;
using System.Collections.Generic;
using Accord.Statistics;
using Accord.Statistics.Testing;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts;
using Sailfish.Contracts.Public;

namespace Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;

public class TwoSampleWilcoxonSignedRankTestSailfish : ITwoSampleWilcoxonSignedRankTestSailfish
{
    private readonly ITestPreprocessor preprocessor;

    public TwoSampleWilcoxonSignedRankTestSailfish(ITestPreprocessor preprocessor)
    {
        this.preprocessor = preprocessor;
    }

    public TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, TestSettings settings)
    {
        var sigDig = settings.Round;

        try
        {
            var (preprocessed1, preprocessed2) = preprocessor.PreprocessJointlyWithDownSample(before, after, settings.UseOutlierDetection, 3, int.MaxValue);
            var sample1 = preprocessed1.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed1.RawData;
            var sample2 = preprocessed2.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed2.RawData;

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
            var testResults = new TestResults(
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
            return new TestResultWithOutlierAnalysis(testResults, preprocessed1.OutlierAnalysis, preprocessed2.OutlierAnalysis);
        }
        catch (Exception ex)
        {
            return new TestResultWithOutlierAnalysis(ex);
        }
    }
}