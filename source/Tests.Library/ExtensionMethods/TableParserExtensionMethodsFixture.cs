using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;
using Shouldly;
using Xunit;

namespace Tests.Library.ExtensionMethods;

public class TableParserExtensionMethodsFixture
{
    [Fact]
    public void TableIsParsedCorrectly()
    {
        var selectors = new Expression<Func<SailDiffResult, object>>[]
        {
            m => m.TestCaseId.DisplayName,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanBefore,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanAfter,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianBefore,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianAfter,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.TestStatistic,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.ChangeDescription
        };

        var colSuffixes = new[]
        {
            string.Empty,
            "ms",
            "ms",
            "ms",
            "ms",
            string.Empty,
            string.Empty,
            string.Empty
        };

        var preprocessor = new TestPreprocessor(new SailfishOutlierDetector());

        var result = new StatisticalTestExecutor(
            new MannWhitneyWilcoxonTest(preprocessor),
            new Test(preprocessor),
            new TwoSampleWilcoxonSignedRankTest(preprocessor),
            new KolmogorovSmirnovTest(preprocessor)
        ).ExecuteStatisticalTest(
            new double[] { 2, 2, 4, 4, 5, 5, 6, 7, 6 },
            new double[] { 9, 8, 7, 6, 4, 4, 1, 2, 3, 2 },
            new SailDiffSettings(0.01, 0, false, TestType.Test));

        TestCaseId testCaseId = new("MyClass.MySampleTest(N: 2, X: 4)");
        var testCaseResults = new List<SailDiffResult> { new(testCaseId, result) };

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