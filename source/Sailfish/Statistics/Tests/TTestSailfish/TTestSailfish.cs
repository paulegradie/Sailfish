using System;
using System.Collections.Generic;
using Accord.Statistics.Testing;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts;
using Sailfish.Contracts.Public;

namespace Sailfish.Statistics.Tests.TTestSailfish;

internal class TTestSailfish : ITTestSailfish
{
    private readonly ITestPreprocessor preprocessor;

    public TTestSailfish(ITestPreprocessor preprocessor)
    {
        this.preprocessor = preprocessor;
    }

    public class AdditionalResults
    {
        public const string DegreesOfFreedom = "DegreesOfFreedom";
    }

    public TestResultWithOutlierAnalysis ExecuteTest(double[] before, double[] after, SailDiffSettings settings)
    {
        var sigDig = settings.Round;

        try
        {
            var preprocessed1 = preprocessor.Preprocess(before, settings.UseOutlierDetection);
            var preprocessed2 = preprocessor.Preprocess(after, settings.UseOutlierDetection);
            var sample1 = preprocessed1.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed1.RawData;
            var sample2 = preprocessed2.OutlierAnalysis?.DataWithOutliersRemoved ?? preprocessed2.RawData;
            var test = new TwoSampleTTest(sample1, sample2, false);
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
            var additionalResults = new Dictionary<string, object>()
            {
                { AdditionalResults.DegreesOfFreedom, dof }
            };
            var testResults = new TestResults(
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
}