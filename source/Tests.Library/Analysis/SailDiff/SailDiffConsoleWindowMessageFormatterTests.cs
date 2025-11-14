using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NSubstitute;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class SailDiffConsoleWindowMessageFormatterTests
{
    private readonly ISailDiffResultMarkdownConverter _mockMarkdownConverter;
    private readonly ILogger _mockLogger;
    private readonly SailDiffConsoleWindowMessageFormatter _formatter;

    public SailDiffConsoleWindowMessageFormatterTests()
    {
        _mockMarkdownConverter = Substitute.For<ISailDiffResultMarkdownConverter>();
        _mockLogger = Substitute.For<ILogger>();
        _formatter = new SailDiffConsoleWindowMessageFormatter(_mockMarkdownConverter, _mockLogger);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Arrange & Act
        var instance = new SailDiffConsoleWindowMessageFormatter(_mockMarkdownConverter, _mockLogger);

        // Assert
        instance.ShouldNotBeNull();
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithSingleResult_ShouldFormatCorrectly()
    {
        // Arrange
        const string expectedMarkdown = "| Test | Before | After |\n|------|--------|-------|";
        _mockMarkdownConverter.ConvertToEnhancedMarkdownTable(Arg.Any<IEnumerable<SailDiffResult>>(), OutputContext.Console)
            .Returns(expectedMarkdown);

        var testCaseId = new TestCaseId("TestClass.TestMethod()");
        var statisticalTestResult = CreateStatisticalTestResult();
        var testResult = new TestResultWithOutlierAnalysis(statisticalTestResult, null, null);
        var sailDiffResult = new SailDiffResult(testCaseId, testResult);
        var sailDiffResults = new[] { sailDiffResult };

        var testIds = new TestIds(["Before.Test1"], ["After.Test1"]);
        var settings = new SailDiffSettings(alpha: 0.001, testType: TestType.TwoSampleWilcoxonSignedRankTest);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("TwoSampleWilcoxonSignedRankTest results comparing:");
        result.ShouldContain("Before: Before.Test1");
        result.ShouldContain("After: After.Test1");
        result.ShouldContain("Note: Changes are significant if the PValue is less than 0.001");
        result.ShouldContain(expectedMarkdown);
        result.ShouldContain("-----------------------------------");
        result.ShouldContain("-----------------------------------\r");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_ShouldCallMarkdownConverter()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = CreateSampleTestIds();
        var settings = CreateSampleSailDiffSettings();

        // Act
        _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        _mockMarkdownConverter.Received(1).ConvertToEnhancedMarkdownTable(sailDiffResults, OutputContext.Console);
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_ShouldLogResult()
    {
        // Arrange
        const string expectedMarkdown = "markdown content";
        _mockMarkdownConverter.ConvertToEnhancedMarkdownTable(Arg.Any<IEnumerable<SailDiffResult>>(), OutputContext.Console)
            .Returns(expectedMarkdown);

        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = CreateSampleTestIds();
        var settings = CreateSampleSailDiffSettings();

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        _mockLogger.Received(1).Log(LogLevel.Information, result);
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithEmptyResults_ShouldStillFormatHeader()
    {
        // Arrange
        const string expectedMarkdown = "";
        _mockMarkdownConverter.ConvertToEnhancedMarkdownTable(Arg.Any<IEnumerable<SailDiffResult>>(), OutputContext.Console)
            .Returns(expectedMarkdown);

        var emptyResults = new List<SailDiffResult>();
        var testIds = new TestIds(["Before.Test1"], ["After.Test1"]);
        var settings = new SailDiffSettings(alpha: 0.05, testType: TestType.Test);

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(emptyResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldContain("Test results comparing:");
        result.ShouldContain("Before: Before.Test1");
        result.ShouldContain("After: After.Test1");
        result.ShouldContain("Note: Changes are significant if the PValue is less than 0.05");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithMultipleBeforeAndAfterIds_ShouldJoinWithCommas()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = new TestIds(
            ["Before.Test1", "Before.Test2", "Before.Test3"],
            ["After.Test1", "After.Test2"]);
        var settings = CreateSampleSailDiffSettings();

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldContain("Before: Before.Test1, Before.Test2, Before.Test3");
        result.ShouldContain("After: After.Test1, After.Test2");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithDifferentTestTypes_ShouldFormatCorrectly()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = CreateSampleTestIds();

        var testCases = new[]
        {
            (TestType.TwoSampleWilcoxonSignedRankTest, "TwoSampleWilcoxonSignedRankTest"),
            (TestType.WilcoxonRankSumTest, "WilcoxonRankSumTest"),
            (TestType.Test, "Test"),
            (TestType.KolmogorovSmirnovTest, "KolmogorovSmirnovTest")
        };

        foreach (var (testType, expectedName) in testCases)
        {
            var settings = new SailDiffSettings(testType: testType);

            // Act
            var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

            // Assert
            result.ShouldContain($"{expectedName} results comparing:");
        }
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithDifferentAlphaValues_ShouldFormatCorrectly()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = CreateSampleTestIds();

        var alphaValues = new[] { 0.001, 0.01, 0.05, 0.1 };

        foreach (var alpha in alphaValues)
        {
            var settings = new SailDiffSettings(alpha: alpha);

            // Act
            var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

            // Assert
            result.ShouldContain($"Note: Changes are significant if the PValue is less than {alpha}");
        }
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithEmptyTestIds_ShouldHandleGracefully()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = new TestIds(new string[0], new string[0]);
        var settings = CreateSampleSailDiffSettings();

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldContain("Before: ");
        result.ShouldContain("After: ");
        result.ShouldNotBeNull();
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_ShouldReturnSameValueAsLogged()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = CreateSampleTestIds();
        var settings = CreateSampleSailDiffSettings();
        string? loggedValue = null;

        _mockLogger.When(x => x.Log(LogLevel.Information, Arg.Any<string>()))
            .Do(callInfo => loggedValue = callInfo.ArgAt<string>(1));

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldBe(loggedValue);
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithSpecialCharactersInTestIds_ShouldHandleCorrectly()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = new TestIds(
            ["Test.With.Dots", "Test(WithParens)", "Test<WithGenerics>"],
            ["Test_With_Underscores", "Test-With-Dashes"]);
        var settings = CreateSampleSailDiffSettings();

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldContain("Before: Test.With.Dots, Test(WithParens), Test<WithGenerics>");
        result.ShouldContain("After: Test_With_Underscores, Test-With-Dashes");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_HeaderFormat_ShouldMatchExpectedStructure()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = new TestIds(["BeforeTest"], ["AfterTest"]);
        var settings = new SailDiffSettings(alpha: 0.001, testType: TestType.Test);

        
        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        var lines = result.Split('\n');
        // Replace strict equality with a normalized check:
        lines[0].Trim().ShouldBe(string.Empty);
        lines[1].Trim().ShouldBe("-----------------------------------");
        lines[2].Trim().ShouldBe("Test results comparing:");
        lines[3].Trim().ShouldBe("Before: BeforeTest");
        lines[4].Trim().ShouldBe("After: AfterTest");
        lines[5].Trim().ShouldBe("-----------------------------------");
        lines[6].Trim().ShouldBe("Note: Changes are significant if the PValue is less than 0.001");
    }

    private static List<SailDiffResult> CreateSampleSailDiffResults()
    {
        var testCaseId = new TestCaseId("SampleTest.Method()");
        var statisticalTestResult = CreateStatisticalTestResult();
        var testResult = new TestResultWithOutlierAnalysis(statisticalTestResult, null, null);
        return [new(testCaseId, testResult)];
    }

    private static TestIds CreateSampleTestIds()
    {
        return new TestIds(["Before.Test1"], ["After.Test1"]);
    }

    private static SailDiffSettings CreateSampleSailDiffSettings()
    {
        return new SailDiffSettings(alpha: 0.001, testType: TestType.TwoSampleWilcoxonSignedRankTest);
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithMultipleResults_ShouldProcessAll()
    {
        // Arrange
        const string expectedMarkdown = "multiple results markdown";
        _mockMarkdownConverter.ConvertToEnhancedMarkdownTable(Arg.Any<IEnumerable<SailDiffResult>>(), OutputContext.Console)
            .Returns(expectedMarkdown);

        var results = new List<SailDiffResult>();
        for (int i = 1; i <= 3; i++)
        {
            var testCaseId = new TestCaseId($"TestClass.TestMethod{i}()");
            var statisticalTestResult = CreateStatisticalTestResult();
            var testResult = new TestResultWithOutlierAnalysis(statisticalTestResult, null, null);
            results.Add(new SailDiffResult(testCaseId, testResult));
        }

        var testIds = CreateSampleTestIds();
        var settings = CreateSampleSailDiffSettings();

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(results, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldContain(expectedMarkdown);
        _mockMarkdownConverter.Received(1).ConvertToEnhancedMarkdownTable(Arg.Is<IEnumerable<SailDiffResult>>(
            r => r.ToList().Count == 3), OutputContext.Console);
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithCancellationToken_ShouldNotThrow()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = CreateSampleTestIds();
        var settings = CreateSampleSailDiffSettings();
        var cancellationToken = new CancellationToken(canceled: true);

        // Act & Assert
        Should.NotThrow(() => _formatter.FormConsoleWindowMessageForSailDiff(
            sailDiffResults, testIds, settings, cancellationToken));
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithLongTestIds_ShouldFormatCorrectly()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var longBeforeId = "Very.Long.Namespace.With.Many.Parts.TestClass.VeryLongMethodNameThatExceedsNormalLength()";
        var longAfterId = "Another.Very.Long.Namespace.With.Many.Parts.TestClass.AnotherVeryLongMethodName()";
        var testIds = new TestIds([longBeforeId], [longAfterId]);
        var settings = CreateSampleSailDiffSettings();

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldContain($"Before: {longBeforeId}");
        result.ShouldContain($"After: {longAfterId}");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithExceptionInTestResult_ShouldStillFormat()
    {
        // Arrange
        var testCaseId = new TestCaseId("TestClass.TestMethod()");
        var exception = new InvalidOperationException("Test exception");
        var testResult = new TestResultWithOutlierAnalysis(exception);
        var sailDiffResults = new[] { new SailDiffResult(testCaseId, testResult) };
        var testIds = CreateSampleTestIds();
        var settings = CreateSampleSailDiffSettings();

        const string expectedMarkdown = "exception result markdown";
        _mockMarkdownConverter.ConvertToEnhancedMarkdownTable(Arg.Any<IEnumerable<SailDiffResult>>(), OutputContext.Console)
            .Returns(expectedMarkdown);

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain(expectedMarkdown);
        _mockMarkdownConverter.Received(1).ConvertToEnhancedMarkdownTable(sailDiffResults, OutputContext.Console);
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithPreciseAlphaValue_ShouldFormatCorrectly()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = CreateSampleTestIds();
        var settings = new SailDiffSettings(alpha: 0.00001);

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldContain("Note: Changes are significant if the PValue is less than 1E-05");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithSingleTestId_ShouldNotIncludeCommas()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = new TestIds(["SingleBeforeTest"], ["SingleAfterTest"]);
        var settings = CreateSampleSailDiffSettings();

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldContain("Before: SingleBeforeTest");
        result.ShouldContain("After: SingleAfterTest");
        result.ShouldNotContain("Before: SingleBeforeTest,");
        result.ShouldNotContain("After: SingleAfterTest,");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_ShouldIncludeCarriageReturnInSeparator()
    {
        // Arrange
        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = CreateSampleTestIds();
        var settings = CreateSampleSailDiffSettings();

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldContain("-----------------------------------\r\n");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailDiff_WithNullMarkdownResult_ShouldHandleGracefully()
    {
        // Arrange
        _mockMarkdownConverter.ConvertToEnhancedMarkdownTable(Arg.Any<IEnumerable<SailDiffResult>>(), OutputContext.Console)
            .Returns((string?)null);

        var sailDiffResults = CreateSampleSailDiffResults();
        var testIds = CreateSampleTestIds();
        var settings = CreateSampleSailDiffSettings();

        // Act
        var result = _formatter.FormConsoleWindowMessageForSailDiff(sailDiffResults, testIds, settings, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("TwoSampleWilcoxonSignedRankTest results comparing:");
    }

    private static StatisticalTestResult CreateStatisticalTestResult()
    {
        return new StatisticalTestResult(
            meanBefore: 10.0,
            meanAfter: 12.0,
            medianBefore: 9.5,
            medianAfter: 11.5,
            testStatistic: 2.5,
            pValue: 0.05,
            changeDescription: SailfishChangeDirection.NoChange,
            sampleSizeBefore: 100,
            sampleSizeAfter: 100,
            rawDataBefore: [8.0, 9.0, 10.0, 11.0, 12.0],
            rawDataAfter: [10.0, 11.0, 12.0, 13.0, 14.0],
            additionalResults: new Dictionary<string, object>());
    }
}
