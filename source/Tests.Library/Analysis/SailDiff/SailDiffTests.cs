using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Logging;
using Sailfish.Presentation.Console;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

public class SailDiffTests
{
    private readonly IMediator _mockMediator;
    private readonly IRunSettings _mockRunSettings;
    private readonly ILogger _mockLogger;
    private readonly IStatisticalTestComputer _mockStatisticalTestComputer;
    private readonly ISailDiffConsoleWindowMessageFormatter _mockSailDiffConsoleWindowMessageFormatter;
    private readonly IConsoleWriter _mockConsoleWriter;
    private readonly Sailfish.Analysis.SailDiff.SailDiff _sailDiff;

    public SailDiffTests()
    {
        _mockMediator = Substitute.For<IMediator>();
        _mockRunSettings = Substitute.For<IRunSettings>();
        _mockLogger = Substitute.For<ILogger>();
        _mockStatisticalTestComputer = Substitute.For<IStatisticalTestComputer>();
        _mockSailDiffConsoleWindowMessageFormatter = Substitute.For<ISailDiffConsoleWindowMessageFormatter>();
        _mockConsoleWriter = Substitute.For<IConsoleWriter>();

        _sailDiff = new Sailfish.Analysis.SailDiff.SailDiff(
            _mockMediator,
            _mockRunSettings,
            _mockLogger,
            _mockStatisticalTestComputer,
            _mockSailDiffConsoleWindowMessageFormatter,
            _mockConsoleWriter);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Arrange & Act
        var instance = new Sailfish.Analysis.SailDiff.SailDiff(
            _mockMediator,
            _mockRunSettings,
            _mockLogger,
            _mockStatisticalTestComputer,
            _mockSailDiffConsoleWindowMessageFormatter,
            _mockConsoleWriter);

        // Assert
        instance.ShouldNotBeNull();
        instance.ShouldBeAssignableTo<ISailDiffInternal>();
        instance.ShouldBeAssignableTo<ISailDiff>();
    }

    [Fact]
    public async Task Analyze_WithTestData_ShouldComputeFormatAndPublish()
    {
        // Arrange
        var beforeData = CreateTestData();
        var afterData = CreateTestData();
        var settings = CreateSailDiffSettings();

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        _mockStatisticalTestComputer
            .ComputeTest(beforeData, afterData, settings)
            .Returns(testResults);

        const string expectedMarkdown = "runtime markdown";
        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(testResults, Arg.Any<TestIds>(), settings, Arg.Any<CancellationToken>())
            .Returns(expectedMarkdown);

        // Act
        _sailDiff.Analyze(beforeData, afterData, settings);

        // Assert
        _mockStatisticalTestComputer.Received(1)
            .ComputeTest(beforeData, afterData, settings);

        _mockSailDiffConsoleWindowMessageFormatter.Received(1)
            .FormConsoleWindowMessageForSailDiff(
                testResults,
                Arg.Is<TestIds>(ids => ids.BeforeTestIds.SequenceEqual(beforeData.TestIds) && ids.AfterTestIds.SequenceEqual(afterData.TestIds)),
                settings,
                Arg.Is<CancellationToken>(ct => ct == CancellationToken.None));

        await _mockMediator.Received(1).Publish(
            Arg.Is<SailDiffAnalysisCompleteNotification>(n =>
                n.TestCaseResults.SequenceEqual(testResults) && n.ResultsAsMarkdown == expectedMarkdown),
            Arg.Is<CancellationToken>(ct => ct == CancellationToken.None));
    }

    [Fact]
    public async Task Analyze_WhenRunSailDiffIsFalse_ShouldReturnEarly()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(false);
        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        await _mockMediator.DidNotReceive().Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyze_WhenNoBeforeFilePaths_ShouldLogWarning()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string>(), // Empty before paths
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns([CreateSailDiffResult()]);

        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        _mockLogger.Received().Log(LogLevel.Warning, "{Message}", Arg.Is<object[]>(args =>
            args.Length == 1 && args[0].ToString()!.Contains("No 'Before' file locations discovered")));
    }

    [Fact]
    public async Task Analyze_WhenNoAfterFilePaths_ShouldLogWarning()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string>()); // Empty after paths

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns([CreateSailDiffResult()]);

        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        _mockLogger.Received().Log(LogLevel.Warning, "{Message}", Arg.Is<object[]>(args =>
            args.Length == 1 && args[0].ToString()!.Contains("No 'After' file locations discovered")));
    }

    [Fact]
    public async Task Analyze_WhenBothBeforeAndAfterFilePathsEmpty_ShouldLogWarningWithBothMessages()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string>(), // Empty before paths
            new List<string>()); // Empty after paths

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns([CreateSailDiffResult()]);

        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        _mockLogger.Received().Log(LogLevel.Warning, "{Message}", Arg.Is<object[]>(args =>
            args.Length == 1 && args[0].ToString()!.Contains("No 'Before' file locations discovered") &&
            args[0].ToString()!.Contains("No 'After' file locations discovered")));
    }

    [Fact]
    public async Task Analyze_WhenBeforeDataIsNull_ShouldLogWarningAndReturn()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = new ReadInBeforeAndAfterDataResponse(null, CreateTestData()); // BeforeData is null
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        _mockLogger.Received().Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
        _mockStatisticalTestComputer.DidNotReceive().ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>());
    }

    [Fact]
    public async Task Analyze_WhenAfterDataIsNull_ShouldLogWarningAndReturn()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = new ReadInBeforeAndAfterDataResponse(CreateTestData(), null); // AfterData is null
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        _mockLogger.Received().Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
        _mockStatisticalTestComputer.DidNotReceive().ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>());
    }

    [Fact]
    public async Task Analyze_WhenBothBeforeAndAfterDataAreNull_ShouldLogWarningAndReturn()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = new ReadInBeforeAndAfterDataResponse(null, null); // Both are null
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        _mockLogger.Received().Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
        _mockStatisticalTestComputer.DidNotReceive().ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>());
    }

    [Fact]
    public async Task Analyze_WhenNoTestResults_ShouldLogInformationAndReturn()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        _mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns([]); // Empty results

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        _mockLogger.Received().Log(LogLevel.Information, "No prior test results found for the current set");
        _mockSailDiffConsoleWindowMessageFormatter.DidNotReceive()
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>());
    }

    // Helper methods for creating test data
    private static TestData CreateTestData()
    {
        var performanceResults = new List<PerformanceRunResult>
        {
            CreatePerformanceRunResult("TestMethod1"),
            CreatePerformanceRunResult("TestMethod2")
        };

        return new TestData(["TestId1", "TestId2"], performanceResults);
    }

    private static PerformanceRunResult CreatePerformanceRunResult(string displayName)
    {
        return new PerformanceRunResult(
            displayName,
            2.5,  // mean
            1.5,  // stdDev
            3.5,  // variance
            2.0,  // median
            [1.0, 2.0, 3.0, 4.0, 5.0], // rawExecutionResults
            5,    // sampleSize
            2,    // numWarmupIterations
            [1.0, 2.0, 3.0], // dataWithOutliersRemoved
            [], // upperOutliers
            [], // lowerOutliers
            0);   // totalNumOutliers
    }

    private static SailDiffSettings CreateSailDiffSettings()
    {
        return new SailDiffSettings(alpha: 0.001, testType: TestType.TwoSampleWilcoxonSignedRankTest);
    }

    private static ReadInBeforeAndAfterDataResponse CreateReadInBeforeAndAfterDataResponse()
    {
        return new ReadInBeforeAndAfterDataResponse(CreateTestData(), CreateTestData());
    }

    private static SailDiffResult CreateSailDiffResult()
    {
        var testCaseId = new TestCaseId("TestClass.TestMethod()");
        var statisticalTestResult = CreateStatisticalTestResult();
        var testResult = new TestResultWithOutlierAnalysis(statisticalTestResult, null, null);
        return new SailDiffResult(testCaseId, testResult);
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

    [Fact]
    public async Task Analyze_SuccessfulExecution_ShouldCompleteAllSteps()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        _mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        const string expectedMarkdown = "test markdown results";
        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns(expectedMarkdown);

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        await _mockMediator.Received().Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), cancellationToken);
        await _mockMediator.Received().Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), cancellationToken);
        _mockStatisticalTestComputer.Received().ComputeTest(dataResponse.BeforeData!, dataResponse.AfterData!, _mockRunSettings.SailDiffSettings);
        _mockSailDiffConsoleWindowMessageFormatter.Received()
            .FormConsoleWindowMessageForSailDiff(testResults, Arg.Any<TestIds>(), _mockRunSettings.SailDiffSettings, cancellationToken);
        _mockLogger.Received().Log(LogLevel.Information, expectedMarkdown);
        await _mockMediator.Received().Publish(Arg.Any<SailDiffAnalysisCompleteNotification>(), cancellationToken);
    }

    [Fact]
    public async Task Analyze_WhenMediatorThrowsExceptionOnFileLocationRequest_ShouldPropagateException()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var expectedException = new InvalidOperationException("Mediator error");
        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => _sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WhenMediatorThrowsExceptionOnDataRequest_ShouldPropagateException()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var expectedException = new InvalidOperationException("Data request error");
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => _sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WhenStatisticalTestComputerThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        _mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var expectedException = new InvalidOperationException("Statistical computation error");
        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Throws(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => _sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WhenFormatterThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        _mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        var expectedException = new InvalidOperationException("Formatter error");
        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => _sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WhenMediatorPublishThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        _mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var expectedException = new InvalidOperationException("Publish error");
        _mockMediator.Publish(Arg.Any<SailDiffAnalysisCompleteNotification>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => _sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WithCancellationToken_ShouldPassTokenToAllCalls()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        _mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        await _mockMediator.Received().Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), cancellationToken);
        await _mockMediator.Received().Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), cancellationToken);
        _mockSailDiffConsoleWindowMessageFormatter.Received()
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), cancellationToken);
        await _mockMediator.Received().Publish(Arg.Any<SailDiffAnalysisCompleteNotification>(), cancellationToken);
    }

    [Fact]
    public async Task Analyze_ShouldCreateCorrectTestIds()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        _mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var beforeData = new TestData(["BeforeTest1", "BeforeTest2"], new List<PerformanceRunResult>());
        var afterData = new TestData(["AfterTest1", "AfterTest2"], new List<PerformanceRunResult>());
        var dataResponse = new ReadInBeforeAndAfterDataResponse(beforeData, afterData);
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        _mockSailDiffConsoleWindowMessageFormatter.Received()
            .FormConsoleWindowMessageForSailDiff(
                testResults,
                Arg.Is<TestIds>(ids =>
                    ids.BeforeTestIds.SequenceEqual(beforeData.TestIds) &&
                    ids.AfterTestIds.SequenceEqual(afterData.TestIds)),
                _mockRunSettings.SailDiffSettings,
                cancellationToken);
    }

    [Fact]
    public async Task Analyze_ShouldPublishCorrectNotification()
    {
        // Arrange
        _mockRunSettings.RunSailDiff.Returns(true);
        _mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        _mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        _mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        _mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        _mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        const string expectedMarkdown = "expected markdown results";
        _mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns(expectedMarkdown);

        var cancellationToken = CancellationToken.None;

        // Act
        await _sailDiff.Analyze(cancellationToken);

        // Assert
        await _mockMediator.Received().Publish(
            Arg.Is<SailDiffAnalysisCompleteNotification>(notification =>
                notification.TestCaseResults.SequenceEqual(testResults) &&
                notification.ResultsAsMarkdown == expectedMarkdown),
            cancellationToken);
    }
}
