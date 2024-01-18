using MathNet.Numerics.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;

public interface ITwoSampleWilcoxonSignedRankTest : ITest
{
}

public class TwoSampleWilcoxonSignedRankTest(ITestPreprocessor preprocessor) : ITwoSampleWilcoxonSignedRankTest
{
    private readonly ITestPreprocessor preprocessor = preprocessor;

    public TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, SailDiffSettings settings)
    {
        var sigDig = settings.Round;

        try
        {
            (var preprocessed1, var preprocessed2) = preprocessor.PreprocessJointlyWithDownSample(before, after, settings.UseOutlierDetection, 3, int.MaxValue);
            var sample1 = preprocessed1.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed1.RawData;
            var sample2 = preprocessed2.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed2.RawData;

            var test = new TwoSampleWilcoxonSignedRank(
                sample1,
                sample2);
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
            var testResults = new StatisticalTestResult(
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