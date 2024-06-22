using System;
using System.Collections.Generic;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Shouldly;
using Tests.Common.Utils;
using Xunit;

namespace Tests.TestAdapter;

public class SailDiffTestOutputWindowMessageFormatterTests
{
    private static readonly string[] afterTestIds = { "Id2" };

    [Fact]
    public void OutputIsCreatedCorrectly()
    {
        var id1 = new TestCaseId("abc.wow()");
        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(new StatisticalTestResult(
            5.0,
            6.0,
            5.0,
            5.0,
            345,
            0.001,
            SailfishChangeDirection.NoChange,
            3,
            3,
            new[] { 1.0, 2, 3 },
            new[] { 9.0, 10, 11 },
            new Dictionary<string, object>()), null, null);

        var sailDiffResult = new SailDiffResult(id1, testResultWithOutlierAnalysis);
        var ids = new TestIds(new[] { id1.DisplayName }, afterTestIds);
        var settings = new SailDiffSettings();

        var formatter = new SailDiffTestOutputWindowMessageFormatter();
        var outputResult = formatter.FormTestOutputWindowMessageForSailDiff(sailDiffResult, ids, settings);

        // don't bother converting this to raw string
        const string expected = @"Before Ids: abc.wow()
After Ids: Id2
Statistical Test
----------------
Test Used:       TwoSampleWilcoxonSignedRankTest
PVal Threshold:  0.001
PValue:          0.001
Change:          No Change  (reason: 0.001 > 0.001)

|             | Before (ms) | After (ms) | 
| ---         | ---         | ---        | 
| Mean        |           5 |          6 | 
| Median      |           5 |          5 | 
| Sample Size |           3 |          3 | 

";
        outputResult.ShouldBe(expected);
    }

    [Fact]
    public void OutputIsNotWrittenWhenThereIsATestFailure()
    {
        var id1 = new TestCaseId("abc.wow()");
        const string exceptionMessage = "Exception Encountered";
        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(new Exception(exceptionMessage));

        var sailDiffResult = new SailDiffResult(id1, testResultWithOutlierAnalysis);
        var ids = new TestIds(new[] { id1.DisplayName }, new[] { Some.RandomString() });
        var settings = new SailDiffSettings();

        var formatter = new SailDiffTestOutputWindowMessageFormatter();
        var outputResult = formatter.FormTestOutputWindowMessageForSailDiff(sailDiffResult, ids, settings);

        const string expected = @$"Statistical testing failed:
{exceptionMessage}
";
        outputResult.ShouldBe(expected);
    }
}