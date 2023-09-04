using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public;
using Sailfish.Extensions.Methods;
using Sailfish.MathOps;
using Sailfish.Statistics.Tests;
using Sailfish.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Statistics.Tests.TTestSailfish;
using Sailfish.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Shouldly;
using Xunit;

namespace Test.ExtensionMethods;

public class TableParserExtensionMethodsFixture
{
    [Fact]
    public void TableIsParsedCorrectly()
    {
        var selectors = new Expression<Func<TestCaseResults, object>>[]
        {
            m => m.TestCaseId.DisplayName,
            m => m.TestResultsWithOutlierAnalysis.TestResults.MeanBefore,
            m => m.TestResultsWithOutlierAnalysis.TestResults.MeanAfter,
            m => m.TestResultsWithOutlierAnalysis.TestResults.MedianBefore,
            m => m.TestResultsWithOutlierAnalysis.TestResults.MedianAfter,
            m => m.TestResultsWithOutlierAnalysis.TestResults.PValue,
            m => m.TestResultsWithOutlierAnalysis.TestResults.TestStatistic,
            m => m.TestResultsWithOutlierAnalysis.TestResults.ChangeDescription
        };

        var colSuffixes = new[]
        {
            "", "ms", "ms", "ms", "ms", "", "", ""
        };

        var preprocessor = new TestPreprocessor(new SailfishOutlierDetector());

        var result = new StatisticalTestExecutor(
            new MannWhitneyWilcoxonTestSailfish(preprocessor),
            new TTestSailfish(preprocessor),
            new TwoSampleWilcoxonSignedRankTestSailfish(preprocessor),
            new KolmogorovSmirnovTestSailfish(preprocessor)
        ).ExecuteStatisticalTest(
            new double[] { 2, 2, 4, 4, 5, 5, 6, 7, 6 },
            new double[] { 9, 8, 7, 6, 4, 4, 1, 2, 3, 2 },
            new SailDiffSettings(0.01, 0, false, TestType.TTest));

        var testCaseId = new TestCaseId("MyClass.MySampleTest(N: 2, X: 4)");
        var testCaseResults = new List<TestCaseResults>() { new(testCaseId, result) };

        var res = testCaseResults
            .ToStringTable(colSuffixes, selectors)
            .Trim()
            .Split(Environment.NewLine)
            .Select(x => x.Trim())
            .ToArray();

        var expected = new[]
        {
            "| DisplayName                      | MeanBefore | MeanAfter | MedianBefore | MedianAfter |       PValue | TestStatistic | ChangeDescription |",
            "| ---                              | ---        | ---       | ---          | ---         | ---          | ---           | ---               |",
            "| MyClass.MySampleTest(N: 2, X: 4) |       5 ms |      5 ms |         5 ms |        4 ms | 0.9666910366 |            -0 |         No Change |"
        };
        res.ShouldBeEquivalentTo(expected);
    }
}