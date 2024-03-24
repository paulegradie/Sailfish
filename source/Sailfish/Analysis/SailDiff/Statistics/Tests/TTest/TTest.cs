using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;

public interface ITTest : ITest
{
}

public class TTest(ITestPreprocessor preprocessor) : ITTest
{
    private readonly ITestPreprocessor preprocessor = preprocessor;

    public TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, SailDiffSettings settings)
    {
        var sigDig = settings.Round;

        try
        {
            var preprocessed1 = preprocessor.Preprocess(before, settings.UseOutlierDetection);
            var preprocessed2 = preprocessor.Preprocess(after, settings.UseOutlierDetection);
            var sample1 = preprocessed1.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed1.RawData;
            var sample2 = preprocessed2.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed2.RawData;
            var test = new TwoSampleT(sample1, sample2, false);
            var meanBefore = Math.Round(test.EstimatedValue1, sigDig);
            var meanAfter = Math.Round(test.EstimatedValue2, sigDig);
            var medianBefore = Math.Round(sample1.Median(), sigDig);
            var medianAfter = Math.Round(sample2.Median(), sigDig);
            var testStatistic = Math.Round(test.Statistic, sigDig);
            var dof = Math.Round(test.DegreesOfFreedom, sigDig);
            var isSignificant = test.PValue <= settings.Alpha;
            var pVal = Math.Round(test.PValue, TestConstants.PValueSigDig);
            var changeDirection = meanAfter > meanBefore ? SailfishChangeDirection.Regressed : SailfishChangeDirection.Improved;
            var description = isSignificant ? changeDirection : SailfishChangeDirection.NoChange;
            var additionalResults = new Dictionary<string, object>
            {
                { AdditionalResults.DegreesOfFreedom, dof }
            };
            var testResults = new StatisticalTestResult(
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
            return new TestResultWithOutlierAnalysis(testResults, preprocessed1.OutlierAnalysis, preprocessed2.OutlierAnalysis);
        }
        catch (Exception ex)
        {
            return new TestResultWithOutlierAnalysis(ex);
        }
    }

    public class AdditionalResults
    {
        public const string DegreesOfFreedom = "DegreesOfFreedom";
    }
}