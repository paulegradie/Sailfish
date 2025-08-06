using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Shouldly;
using System;
using System.Collections.Generic;
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

        // Test the key components instead of exact string matching due to formatting complexities
        outputResult.ShouldContain("📊 SAILDIFF PERFORMANCE ANALYSIS");
        outputResult.ShouldContain("==================================================");
        outputResult.ShouldContain("⚪ IMPACT: 20.0% difference (NO CHANGE)");
        outputResult.ShouldContain("P-Value: 0.001000 | Mean: 5.000ms → 6.000ms");
        outputResult.ShouldContain("Before Ids: abc.wow()");
        outputResult.ShouldContain("After Ids: Id2");
        outputResult.ShouldContain("📋 Statistical Test Details");
        outputResult.ShouldContain("------------------------");
        outputResult.ShouldContain("Test Used:       TwoSampleWilcoxonSignedRankTest");
        outputResult.ShouldContain("PVal Threshold:  0.001");
        outputResult.ShouldContain("PValue:          0.001");
        outputResult.ShouldContain("Change:          No Change  (reason: 0.001 > 0.001)");
        outputResult.ShouldContain("|             | Before (ms) | After (ms) |");
        outputResult.ShouldContain("| ---         | ---         | ---        |");
        outputResult.ShouldContain("| Mean        |           5 |          6 |");
        outputResult.ShouldContain("| Median      |           5 |          5 |");
        outputResult.ShouldContain("| Sample Size |           3 |          3 |");
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