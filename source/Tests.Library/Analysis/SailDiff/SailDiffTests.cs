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
    private readonly IMediator mockMediator;
    private readonly IRunSettings mockRunSettings;
    private readonly ILogger mockLogger;
    private readonly IStatisticalTestComputer mockStatisticalTestComputer;
    private readonly ISailDiffConsoleWindowMessageFormatter mockSailDiffConsoleWindowMessageFormatter;
    private readonly IConsoleWriter mockConsoleWriter;
    private readonly Sailfish.Analysis.SailDiff.SailDiff sailDiff;

    public SailDiffTests()
    {
        mockMediator = Substitute.For<IMediator>();
        mockRunSettings = Substitute.For<IRunSettings>();
        mockLogger = Substitute.For<ILogger>();
        mockStatisticalTestComputer = Substitute.For<IStatisticalTestComputer>();
        mockSailDiffConsoleWindowMessageFormatter = Substitute.For<ISailDiffConsoleWindowMessageFormatter>();
        mockConsoleWriter = Substitute.For<IConsoleWriter>();

        sailDiff = new Sailfish.Analysis.SailDiff.SailDiff(
            mockMediator,
            mockRunSettings,
            mockLogger,
            mockStatisticalTestComputer,
            mockSailDiffConsoleWindowMessageFormatter,
            mockConsoleWriter);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Arrange & Act
        var instance = new Sailfish.Analysis.SailDiff.SailDiff(
            mockMediator,
            mockRunSettings,
            mockLogger,
            mockStatisticalTestComputer,
            mockSailDiffConsoleWindowMessageFormatter,
            mockConsoleWriter);

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
        mockStatisticalTestComputer
            .ComputeTest(beforeData, afterData, settings)
            .Returns(testResults);

        const string expectedMarkdown = "runtime markdown";
        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(testResults, Arg.Any<TestIds>(), settings, Arg.Any<CancellationToken>())
            .Returns(expectedMarkdown);

        // Act
        sailDiff.Analyze(beforeData, afterData, settings);

        // Assert
        mockStatisticalTestComputer.Received(1)
            .ComputeTest(beforeData, afterData, settings);

        mockSailDiffConsoleWindowMessageFormatter.Received(1)
            .FormConsoleWindowMessageForSailDiff(
                testResults,
                Arg.Is<TestIds>(ids => ids.BeforeTestIds.SequenceEqual(beforeData.TestIds) && ids.AfterTestIds.SequenceEqual(afterData.TestIds)),
                settings,
                Arg.Is<CancellationToken>(ct => ct == CancellationToken.None));

        await mockMediator.Received(1).Publish(
            Arg.Is<Sailfish.Contracts.Public.Notifications.SailDiffAnalysisCompleteNotification>(n =>
                n.TestCaseResults.SequenceEqual(testResults) && n.ResultsAsMarkdown == expectedMarkdown),
            Arg.Is<CancellationToken>(ct => ct == CancellationToken.None));
    }

    [Fact]
    public async Task Analyze_WhenRunSailDiffIsFalse_ShouldReturnEarly()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(false);
        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        await mockMediator.DidNotReceive().Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Analyze_WhenNoBeforeFilePaths_ShouldLogWarning()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string>(), // Empty before paths
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns([CreateSailDiffResult()]);

        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        mockLogger.Received().Log(LogLevel.Warning, "{Message}", Arg.Is<object[]>(args =>
            args.Length == 1 && args[0].ToString()!.Contains("No 'Before' file locations discovered")));
    }

    [Fact]
    public async Task Analyze_WhenNoAfterFilePaths_ShouldLogWarning()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string>()); // Empty after paths

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns([CreateSailDiffResult()]);

        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        mockLogger.Received().Log(LogLevel.Warning, "{Message}", Arg.Is<object[]>(args =>
            args.Length == 1 && args[0].ToString()!.Contains("No 'After' file locations discovered")));
    }

    [Fact]
    public async Task Analyze_WhenBothBeforeAndAfterFilePathsEmpty_ShouldLogWarningWithBothMessages()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string>(), // Empty before paths
            new List<string>()); // Empty after paths

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns([CreateSailDiffResult()]);

        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        mockLogger.Received().Log(LogLevel.Warning, "{Message}", Arg.Is<object[]>(args =>
            args.Length == 1 && args[0].ToString()!.Contains("No 'Before' file locations discovered") &&
            args[0].ToString()!.Contains("No 'After' file locations discovered")));
    }

    [Fact]
    public async Task Analyze_WhenBeforeDataIsNull_ShouldLogWarningAndReturn()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = new ReadInBeforeAndAfterDataResponse(null, CreateTestData()); // BeforeData is null
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        mockLogger.Received().Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
        mockStatisticalTestComputer.DidNotReceive().ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>());
    }

    [Fact]
    public async Task Analyze_WhenAfterDataIsNull_ShouldLogWarningAndReturn()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = new ReadInBeforeAndAfterDataResponse(CreateTestData(), null); // AfterData is null
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        mockLogger.Received().Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
        mockStatisticalTestComputer.DidNotReceive().ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>());
    }

    [Fact]
    public async Task Analyze_WhenBothBeforeAndAfterDataAreNull_ShouldLogWarningAndReturn()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = new ReadInBeforeAndAfterDataResponse(null, null); // Both are null
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        mockLogger.Received().Log(LogLevel.Warning, "Failed to retrieve tracking data... aborting the test operation");
        mockStatisticalTestComputer.DidNotReceive().ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>());
    }

    [Fact]
    public async Task Analyze_WhenNoTestResults_ShouldLogInformationAndReturn()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns([]); // Empty results

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        mockLogger.Received().Log(LogLevel.Information, "No prior test results found for the current set");
        mockSailDiffConsoleWindowMessageFormatter.DidNotReceive()
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
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        const string expectedMarkdown = "test markdown results";
        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns(expectedMarkdown);

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        await mockMediator.Received().Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), cancellationToken);
        await mockMediator.Received().Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), cancellationToken);
        mockStatisticalTestComputer.Received().ComputeTest(dataResponse.BeforeData!, dataResponse.AfterData!, mockRunSettings.SailDiffSettings);
        mockSailDiffConsoleWindowMessageFormatter.Received()
            .FormConsoleWindowMessageForSailDiff(testResults, Arg.Any<TestIds>(), mockRunSettings.SailDiffSettings, cancellationToken);
        mockLogger.Received().Log(LogLevel.Information, expectedMarkdown);
        await mockMediator.Received().Publish(Arg.Any<SailDiffAnalysisCompleteNotification>(), cancellationToken);
    }

    [Fact]
    public async Task Analyze_WhenMediatorThrowsExceptionOnFileLocationRequest_ShouldPropagateException()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var expectedException = new InvalidOperationException("Mediator error");
        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WhenMediatorThrowsExceptionOnDataRequest_ShouldPropagateException()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var expectedException = new InvalidOperationException("Data request error");
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WhenStatisticalTestComputerThrowsException_ShouldPropagateException()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var expectedException = new InvalidOperationException("Statistical computation error");
        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Throws(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WhenFormatterThrowsException_ShouldPropagateException()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        var expectedException = new InvalidOperationException("Formatter error");
        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WhenMediatorPublishThrowsException_ShouldPropagateException()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var expectedException = new InvalidOperationException("Publish error");
        mockMediator.Publish(Arg.Any<SailDiffAnalysisCompleteNotification>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => sailDiff.Analyze(cancellationToken));
        exception.ShouldBe(expectedException);
    }

    [Fact]
    public async Task Analyze_WithCancellationToken_ShouldPassTokenToAllCalls()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        await mockMediator.Received().Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), cancellationToken);
        await mockMediator.Received().Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), cancellationToken);
        mockSailDiffConsoleWindowMessageFormatter.Received()
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), cancellationToken);
        await mockMediator.Received().Publish(Arg.Any<SailDiffAnalysisCompleteNotification>(), cancellationToken);
    }

    [Fact]
    public async Task Analyze_ShouldCreateCorrectTestIds()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var beforeData = new TestData(["BeforeTest1", "BeforeTest2"], new List<PerformanceRunResult>());
        var afterData = new TestData(["AfterTest1", "AfterTest2"], new List<PerformanceRunResult>());
        var dataResponse = new ReadInBeforeAndAfterDataResponse(beforeData, afterData);
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns("test markdown");

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        mockSailDiffConsoleWindowMessageFormatter.Received()
            .FormConsoleWindowMessageForSailDiff(
                testResults,
                Arg.Is<TestIds>(ids =>
                    ids.BeforeTestIds.SequenceEqual(beforeData.TestIds) &&
                    ids.AfterTestIds.SequenceEqual(afterData.TestIds)),
                mockRunSettings.SailDiffSettings,
                cancellationToken);
    }

    [Fact]
    public async Task Analyze_ShouldPublishCorrectNotification()
    {
        // Arrange
        mockRunSettings.RunSailDiff.Returns(true);
        mockRunSettings.ProvidedBeforeTrackingFiles.Returns(new List<string>());
        mockRunSettings.SailDiffSettings.Returns(CreateSailDiffSettings());

        var fileLocationResponse = new BeforeAndAfterFileLocationResponse(
            new List<string> { "before.json" },
            new List<string> { "after.json" });

        mockMediator.Send(Arg.Any<BeforeAndAfterFileLocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(fileLocationResponse);

        var dataResponse = CreateReadInBeforeAndAfterDataResponse();
        mockMediator.Send(Arg.Any<ReadInBeforeAndAfterDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(dataResponse);

        var testResults = new List<SailDiffResult> { CreateSailDiffResult() };
        mockStatisticalTestComputer.ComputeTest(Arg.Any<TestData>(), Arg.Any<TestData>(), Arg.Any<SailDiffSettings>())
            .Returns(testResults);

        const string expectedMarkdown = "expected markdown results";
        mockSailDiffConsoleWindowMessageFormatter
            .FormConsoleWindowMessageForSailDiff(Arg.Any<IEnumerable<SailDiffResult>>(), Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>(), Arg.Any<CancellationToken>())
            .Returns(expectedMarkdown);

        var cancellationToken = CancellationToken.None;

        // Act
        await sailDiff.Analyze(cancellationToken);

        // Assert
        await mockMediator.Received().Publish(
            Arg.Is<SailDiffAnalysisCompleteNotification>(notification =>
                notification.TestCaseResults.SequenceEqual(testResults) &&
                notification.ResultsAsMarkdown == expectedMarkdown),
            cancellationToken);
    }
}
