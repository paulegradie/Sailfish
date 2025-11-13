using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Mapping;
using Sailfish.TestAdapter.TestProperties;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for TestCompletionMessageMapper service.
/// Tests the mapping of test case completion notifications to queue messages
/// with comprehensive metadata for batch processing and cross-test-case analysis.
/// </summary>
public class TestCompletionMessageMapperTests
{
    private readonly ILogger _logger;
    private readonly ISailfishConsoleWindowFormatter _consoleFormatter;
    private readonly ISailDiffTestOutputWindowMessageFormatter _sailDiffFormatter;
    private readonly IAdapterSailDiff _sailDiff;
    private readonly IRunSettings _runSettings;
    private readonly IMediator _mediator;
    private readonly TestCompletionMessageMapper _mapper;

    public TestCompletionMessageMapperTests()
    {
        _logger = Substitute.For<ILogger>();
        _consoleFormatter = Substitute.For<ISailfishConsoleWindowFormatter>();
        _sailDiffFormatter = Substitute.For<ISailDiffTestOutputWindowMessageFormatter>();
        _sailDiff = Substitute.For<IAdapterSailDiff>();
        _runSettings = Substitute.For<IRunSettings>();
        _mediator = Substitute.For<IMediator>();

        _mapper = new TestCompletionMessageMapper(
            _logger,
            _consoleFormatter,
            _sailDiffFormatter,
            _sailDiff,
            _runSettings,
            _mediator);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionMessageMapper(
            null!,
            _consoleFormatter,
            _sailDiffFormatter,
            _sailDiff,
            _runSettings,
            _mediator));
    }

    [Fact]
    public void Constructor_WithNullConsoleFormatter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionMessageMapper(
            _logger,
            null!,
            _sailDiffFormatter,
            _sailDiff,
            _runSettings,
            _mediator));
    }

    [Fact]
    public void Constructor_WithNullSailDiffFormatter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionMessageMapper(
            _logger,
            _consoleFormatter,
            null!,
            _sailDiff,
            _runSettings,
            _mediator));
    }

    [Fact]
    public void Constructor_WithNullSailDiff_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionMessageMapper(
            _logger,
            _consoleFormatter,
            _sailDiffFormatter,
            null!,
            _runSettings,
            _mediator));
    }

    [Fact]
    public void Constructor_WithNullRunSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionMessageMapper(
            _logger,
            _consoleFormatter,
            _sailDiffFormatter,
            _sailDiff,
            null!,
            _mediator));
    }

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionMessageMapper(
            _logger,
            _consoleFormatter,
            _sailDiffFormatter,
            _sailDiff,
            _runSettings,
            null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        _mapper.ShouldNotBeNull();
    }

    #endregion

    #region MapToQueueMessageAsync Tests

    [Fact]
    public async Task MapToQueueMessageAsync_WithNullNotification_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _mapper.MapToQueueMessageAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithNullTestInstanceContainer_ShouldThrowSailfishException()
    {
        // Arrange
        var baseNotification = CreateTestNotification();
        var notification = new TestCaseCompletedNotification(
            baseNotification.ClassExecutionSummaryTrackingFormat,
            null!,
            baseNotification.TestCaseGroup);

        // Act & Assert
        var exception = await Should.ThrowAsync<SailfishException>(
            () => _mapper.MapToQueueMessageAsync(notification, CancellationToken.None));

        // The exception gets wrapped, so check for the wrapped message pattern
        exception.Message.ShouldContain("Failed to map test case completion notification to queue message", Case.Insensitive);
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithNullPerformanceTimer_ShouldThrowSailfishException()
    {
        // Arrange
        var notification = CreateTestNotification();
        // Cannot modify PerformanceTimer as it's read-only, so create a new notification with null timer
        var testCaseId = new TestCaseId("TestClass1.TestMethod1");
        var executionSettings = Substitute.For<IExecutionSettings>();
        var testInstanceContainer = new TestInstanceContainerExternal(
            typeof(TestCompletionMessageMapperTests),
            this,
            typeof(TestCompletionMessageMapperTests).GetMethod(nameof(CreateTestNotification))!,
            testCaseId,
            executionSettings,
            null!, // null PerformanceTimer to trigger the exception
            false);

        var newNotification = new TestCaseCompletedNotification(
            notification.ClassExecutionSummaryTrackingFormat,
            testInstanceContainer,
            notification.TestCaseGroup);

        // Act & Assert
        var exception = await Should.ThrowAsync<SailfishException>(
            () => _mapper.MapToQueueMessageAsync(newNotification, CancellationToken.None));

        exception.Message.ShouldContain("PerformanceTimerResults was null");
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithValidNotification_ShouldReturnQueueMessage()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.TestCaseId.ShouldBe("TestClass1.TestMethod1()");
        result.TestResult.ShouldNotBeNull();
        result.PerformanceMetrics.ShouldNotBeNull();
        result.Metadata.ShouldNotBeNull();
        result.Metadata.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithSuccessfulTest_ShouldSetCorrectTestResult()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.TestResult.IsSuccess.ShouldBeTrue();
        result.TestResult.ExceptionMessage.ShouldBeNull();
        result.TestResult.ExceptionDetails.ShouldBeNull();
        result.TestResult.ExceptionType.ShouldBeNull();
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithFailedTest_ShouldSetCorrectTestResult()
    {
        // Arrange
        var notification = CreateFailedTestNotification();
        SetupMockDependencies();

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.TestResult.IsSuccess.ShouldBeFalse();
        result.TestResult.ExceptionMessage.ShouldNotBeNull();
        result.TestResult.ExceptionDetails.ShouldNotBeNull();
        result.TestResult.ExceptionType.ShouldNotBeNull();
    }


        [Fact]
        public async Task MapToQueueMessageAsync_AppendsOverheadDiagnostics_WhenBaselineAvailable()
        {
            // Arrange
            var notification = CreateTestNotification();
            SetupMockDependencies();

            var perfTimer = notification.TestInstanceContainerExternal!.PerformanceTimer;
            // Helper to set internal-set properties via reflection
            void SetProp(string name, object? value)
            {
                var prop = typeof(PerformanceTimer).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                prop!.SetValue(perfTimer, value);
            }

            SetProp("OverheadBaselineTicks", 123);
            SetProp("OverheadDriftPercent", 2.56);
            SetProp("OverheadWarmupCount", 3);
            SetProp("OverheadSampleCount", 64);
            SetProp("CappedIterationCount", 5);

            // Act
            var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

            // Assert
            result.ShouldNotBeNull();
            result.Metadata.ShouldContainKey("FormattedMessage");
            var formatted = result.Metadata["FormattedMessage"] as string;
            formatted.ShouldNotBeNull();
            formatted!.ShouldContain("Formatted console message");
            formatted.ShouldContain("Diagnostics: Overhead 123 ticks");
            formatted.ShouldContain("(median of 64 samples; 3 warmup)");
            formatted.ShouldContain("Drift 2.56%");
            formatted.ShouldContain("Capped 5 iteration(s).");

            // Logger receives the same diagnostic line
            _logger.Received().Log(
                LogLevel.Information,
                Arg.Is<string>(s => s.Contains("Diagnostics: Overhead 123 ticks")),
                Arg.Any<object[]>());
        }

        [Fact]
        public async Task MapToQueueMessageAsync_AppendsOverheadDisabledMessage_WhenDisabled()
        {
            // Arrange
            var notification = CreateTestNotification();
            SetupMockDependencies();

            var perfTimer = notification.TestInstanceContainerExternal!.PerformanceTimer;
            void SetProp(string name, object? value)
            {
                var prop = typeof(PerformanceTimer).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                prop!.SetValue(perfTimer, value);
            }

            // Ensure baseline is null and disabled flag is set
            SetProp("OverheadBaselineTicks", null);
            SetProp("OverheadEstimationDisabled", true);

            // Act
            var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

            // Assert
            result.ShouldNotBeNull();
            var formatted = result.Metadata["FormattedMessage"] as string;
            formatted.ShouldNotBeNull();
            const string diag = "Diagnostics: Overhead estimation disabled for this test (no subtraction applied).";
            formatted!.ShouldContain(diag);

            _logger.Received().Log(
                LogLevel.Information,
                Arg.Is<string>(s => s.Contains("Overhead estimation disabled")),
                Arg.Any<object[]>());
        }


    #endregion

    #region SailDiff Integration Tests

    [Fact]
    public async Task MapToQueueMessageAsync_WithSailDiffEnabled_ShouldIncludeSailDiffResults()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        var sailDiffResult = new TestCaseSailDiffResult(
            new List<SailDiffResult> { Substitute.For<SailDiffResult>() },
            new TestIds(new[] { "TestMethod1" }, new[] { "TestMethod1" }),
            new SailDiffSettings());

        _sailDiff.ComputeTestCaseDiff(Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<string>(),
            Arg.Any<IClassExecutionSummary>(), Arg.Any<PerformanceRunResult>())
            .Returns(sailDiffResult);

        _sailDiffFormatter.FormTestOutputWindowMessageForSailDiff(Arg.Any<SailDiffResult>(),
            Arg.Any<TestIds>(), Arg.Any<SailDiffSettings>())
            .Returns("SailDiff analysis results");

        // Setup previous run data with a matching test case
        var trackingData = new TrackingFileDataList();
        var testCaseId = new TestCaseId("TestClass1.TestMethod1");

        // Create a mock compiled result with performance data
        var mockCompiledResult = Substitute.For<ICompiledTestCaseResult>();
        var performanceResult = new PerformanceRunResult(
            testCaseId.DisplayName,
            100.0, // mean
            5.0,   // stdDev
            25.0,  // variance
            95.0,  // median
            new double[] { 90, 95, 100, 105 }, // rawExecutionResults
            4,     // sampleSize
            1,     // numWarmupIterations
            new double[] { 95, 100 }, // dataWithOutliersRemoved
            new double[] { 105 },     // upperOutliers
            new double[] { 90 },      // lowerOutliers
            2      // totalNumOutliers
        );
        mockCompiledResult.PerformanceRunResult.Returns(performanceResult);
        mockCompiledResult.TestCaseId.Returns(testCaseId);

        var classExecutionSummary = Substitute.For<IClassExecutionSummary>();
        classExecutionSummary.CompiledTestCaseResults.Returns(new[] { mockCompiledResult });

        // Add current run (will be skipped by GetLastRunAsync)
        trackingData.Add(new List<IClassExecutionSummary> { classExecutionSummary });
        // Add previous run (will be used for SailDiff analysis)
        trackingData.Add(new List<IClassExecutionSummary> { classExecutionSummary });

        _mediator.Send(Arg.Any<GetAllTrackingDataOrderedChronologicallyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetAllTrackingDataOrderedChronologicallyResponse(trackingData));

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        var formattedMessage = result.Metadata["FormattedMessage"] as string;
        formattedMessage.ShouldNotBeNull();
        formattedMessage!.ShouldContain("SailDiff analysis results");
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithSailDiffDisabled_ShouldNotIncludeSailDiffResults()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();
        _runSettings.DisableAnalysisGlobally.Returns(true);

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        _sailDiff.DidNotReceive().ComputeTestCaseDiff(Arg.Any<string[]>(), Arg.Any<string[]>(),
            Arg.Any<string>(), Arg.Any<IClassExecutionSummary>(), Arg.Any<PerformanceRunResult>());
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithSailDiffException_ShouldReturnOriginalMessage()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        _sailDiff.When(x => x.ComputeTestCaseDiff(Arg.Any<string[]>(), Arg.Any<string[]>(), Arg.Any<string>(),
            Arg.Any<IClassExecutionSummary>(), Arg.Any<PerformanceRunResult>()))
            .Do(x => throw new InvalidOperationException("SailDiff failed"));

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        var formattedMessage = result.Metadata["FormattedMessage"] as string;
        formattedMessage.ShouldBe("Formatted console message");
    }

    #endregion

    #region Performance Metrics Tests

    [Fact]
    public async Task MapToQueueMessageAsync_WithValidPerformanceData_ShouldExtractCorrectMetrics()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.PerformanceMetrics.ShouldNotBeNull();
        result.PerformanceMetrics.MedianMs.ShouldBe(100.0);
        result.PerformanceMetrics.MeanMs.ShouldBe(105.0);
        result.PerformanceMetrics.StandardDeviation.ShouldBe(10.0);
        result.PerformanceMetrics.Variance.ShouldBe(100.0);
        result.PerformanceMetrics.SampleSize.ShouldBe(10);
        result.PerformanceMetrics.NumWarmupIterations.ShouldBe(3);
        result.PerformanceMetrics.RawExecutionResults.ShouldBe(new double[] { 95, 100, 105, 110 });
        result.PerformanceMetrics.TotalNumOutliers.ShouldBe(2);
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithNullPerformanceData_ShouldReturnEmptyMetrics()
    {
        // Arrange
        var notification = CreateTestNotificationWithNullPerformanceData();
        SetupMockDependencies();

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.PerformanceMetrics.ShouldNotBeNull();
        result.PerformanceMetrics.MedianMs.ShouldBe(0);
        result.PerformanceMetrics.MeanMs.ShouldBe(0);
        result.PerformanceMetrics.StandardDeviation.ShouldBe(0);
        result.PerformanceMetrics.Variance.ShouldBe(0);
        result.PerformanceMetrics.SampleSize.ShouldBe(0);
        result.PerformanceMetrics.RawExecutionResults.ShouldBeEmpty();
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithExceptionInTest_ShouldReturnEmptyMetrics()
    {
        // Arrange
        var notification = CreateTestNotificationWithException();
        SetupMockDependencies();

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.PerformanceMetrics.ShouldNotBeNull();
        result.PerformanceMetrics.MedianMs.ShouldBe(0);
        result.PerformanceMetrics.MeanMs.ShouldBe(0);
        result.PerformanceMetrics.RawExecutionResults.ShouldBeEmpty();
    }

    #endregion

    #region Metadata Extraction Tests

    [Fact]
    public async Task MapToQueueMessageAsync_ShouldExtractBatchingMetadata()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.Metadata.ShouldContainKey("Batching_TestClassName");
        result.Metadata.ShouldContainKey("Batching_TestAssemblyName");
        result.Metadata.ShouldContainKey("Batching_TestMethodName");
        result.Metadata.ShouldContainKey("Batching_FullyQualifiedName");
        result.Metadata.ShouldContainKey("Batching_ExecutionContext");
    }

    [Fact]
    public async Task MapToQueueMessageAsync_ShouldExtractCoreMetadata()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.Metadata.ShouldContainKey("TestCase");
        result.Metadata.ShouldContainKey("FormattedMessage");
        result.Metadata.ShouldContainKey("StartTime");
        result.Metadata.ShouldContainKey("EndTime");
        result.Metadata.ShouldContainKey("MedianRuntime");
        result.Metadata.ShouldContainKey("StatusCode");
        result.Metadata.ShouldContainKey("ClassExecutionSummaries");
        result.Metadata.ShouldContainKey("CompiledTestCaseResult");
        result.Metadata.ShouldContainKey("TestCaseGroup");
        result.Metadata.ShouldContainKey("RunSettings");
        result.Metadata.ShouldContainKey("OriginalNotification");
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithComparisonGroup_ShouldExtractComparisonMetadata()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        // Add comparison group property to test case
        var testCase = notification.TestCaseGroup.First() as TestCase;
        testCase!.SetPropertyValue(SailfishManagedProperty.SailfishComparisonGroupProperty, "ComparisonGroup1");
        testCase.SetPropertyValue(SailfishManagedProperty.SailfishComparisonRoleProperty, "Before");

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.Metadata.ShouldContainKey("Batching_ComparisonGroup");
        result.Metadata.ShouldContainKey("Batching_ComparisonRole");
        result.Metadata["Batching_ComparisonGroup"].ShouldBe("ComparisonGroup1");
        result.Metadata["Batching_ComparisonRole"].ShouldBe("Before");
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithCustomTraits_ShouldExtractCustomCriteria()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        // Add custom traits to test case
        var testCase = notification.TestCaseGroup.First() as TestCase;
        testCase!.Traits.Add(new Trait("Category", "Performance"));
        testCase.Traits.Add(new Trait("BatchGroup", "Group1"));

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.Metadata.ShouldContainKey("Batching_CustomBatchingCriteria");
        var customCriteria = result.Metadata["Batching_CustomBatchingCriteria"] as Dictionary<string, object>;
        customCriteria.ShouldNotBeNull();
        customCriteria.ShouldContainKey("Category");
        customCriteria.ShouldContainKey("BatchGroup");
        customCriteria["Category"].ShouldBe("Performance");
        customCriteria["BatchGroup"].ShouldBe("Group1");
    }

    [Fact]
    public async Task MapToQueueMessageAsync_ShouldIncludeExecutionContext()
    {
        // Arrange
        var notification = CreateTestNotification();
        SetupMockDependencies();

        // Act
        var result = await _mapper.MapToQueueMessageAsync(notification, CancellationToken.None);

        // Assert
        result.Metadata.ShouldContainKey("Batching_ExecutionContext");
        var executionContext = result.Metadata["Batching_ExecutionContext"] as Dictionary<string, object>;
        executionContext.ShouldNotBeNull();
        executionContext.ShouldContainKey("DisableAnalysisGlobally");
        executionContext.ShouldContainKey("RunScaleFish");
        executionContext.ShouldContainKey("RunSailDiff");
        executionContext.ShouldContainKey("TestExecutionDateTime");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task MapToQueueMessageAsync_WithMappingException_ShouldThrowSailfishException()
    {
        // Arrange
        var notification = CreateTestNotification();
        _consoleFormatter.FormConsoleWindowMessageForSailfish(Arg.Any<IEnumerable<IClassExecutionSummary>>())
            .Throws(new InvalidOperationException("Formatting failed"));

        // Act & Assert
        var exception = await Should.ThrowAsync<SailfishException>(
            () => _mapper.MapToQueueMessageAsync(notification, CancellationToken.None));

        exception.Message.ShouldContain("Failed to map test case completion notification to queue message");
    }

    [Fact]
    public async Task MapToQueueMessageAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Don't call SetupMockDependencies() to avoid setting up the mediator response
        _consoleFormatter.FormConsoleWindowMessageForSailfish(Arg.Any<IClassExecutionSummary[]>())
            .Returns("Formatted console message");
        _consoleFormatter.FormConsoleWindowMessageForSailfish(Arg.Any<IEnumerable<IClassExecutionSummary>>())
            .Returns("Formatted console message");
        _runSettings.DisableAnalysisGlobally.Returns(false);
        _runSettings.RunScaleFish.Returns(true);
        _runSettings.RunSailDiff.Returns(true);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Setup mediator to throw OperationCanceledException when called with any token
        _mediator.Send(Arg.Any<GetAllTrackingDataOrderedChronologicallyRequest>(), Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => _mapper.MapToQueueMessageAsync(notification, cts.Token));
    }

    #endregion

    #region Helper Methods

    private TestCaseCompletedNotification CreateTestNotification()
    {
        var testCase = new TestCase("TestClass1.TestMethod1()", new Uri("executor://sailfish"), "TestAssembly.dll");
        var testCaseId = new TestCaseId("TestClass1.TestMethod1");

        // Create a real PerformanceTimer instance
        var performanceTimer = new PerformanceTimer();

        // Create a real TestInstanceContainerExternal
        var executionSettings = Substitute.For<IExecutionSettings>();
        var testInstanceContainer = new TestInstanceContainerExternal(
            typeof(TestCompletionMessageMapperTests),
            this,
            typeof(TestCompletionMessageMapperTests).GetMethod(nameof(CreateTestNotification))!,
            testCaseId,
            executionSettings,
            performanceTimer,
            false);

        // Create performance result
        var performanceResult = new PerformanceRunResult(
            testCaseId.DisplayName, // displayName
            105.0, // mean
            10.0,  // stdDev
            100.0, // variance
            100.0, // median
            new double[] { 95, 100, 105, 110 }, // rawExecutionResults
            10,    // sampleSize
            3,     // numWarmupIterations
            new double[] { 100, 105 }, // dataWithOutliersRemoved
            new double[] { 110 },      // upperOutliers
            new double[] { 95 },       // lowerOutliers
            2      // totalNumOutliers
        );

        var compiledResult = new CompiledTestCaseResult(testCaseId, "TestGroup", performanceResult);

        var executionSettingsTracking = new ExecutionSettingsTrackingFormat();
        var performanceResultTracking = new PerformanceRunResultTrackingFormat(
            testCaseId.DisplayName,
            105.0, // mean
            100.0, // median
            10.0,  // stdDev
            100.0, // variance
            new double[] { 95, 100, 105, 110 }, // rawExecutionResults
            10,    // sampleSize
            3,     // numWarmupIterations
            new double[] { 100, 105 }, // dataWithOutliersRemoved
            new double[] { 110 },      // upperOutliers
            new double[] { 95 },       // lowerOutliers
            2      // totalNumOutliers
        );

        var compiledResultTracking = new CompiledTestCaseResultTrackingFormat(
            "TestGroup",
            performanceResultTracking,
            null,
            testCaseId);

        var classExecutionSummary = new ClassExecutionSummaryTrackingFormat(
            typeof(TestCompletionMessageMapperTests),
            executionSettingsTracking,
            new[] { compiledResultTracking });

        return new TestCaseCompletedNotification(
            classExecutionSummary,
            testInstanceContainer,
            new[] { testCase });
    }

    private TestCaseCompletedNotification CreateFailedTestNotification()
    {
        var testCase = new TestCase("TestClass1.TestMethod1()", new Uri("executor://sailfish"), "TestAssembly.dll");
        var testCaseId = new TestCaseId("TestClass1.TestMethod1");

        // Create a real PerformanceTimer instance
        var performanceTimer = new PerformanceTimer();

        // Create a real TestInstanceContainerExternal
        var executionSettings = Substitute.For<IExecutionSettings>();
        var testInstanceContainer = new TestInstanceContainerExternal(
            typeof(TestCompletionMessageMapperTests),
            this,
            typeof(TestCompletionMessageMapperTests).GetMethod(nameof(CreateTestNotification))!,
            testCaseId,
            executionSettings,
            performanceTimer,
            false);

        // Create failed result
        var exception = new InvalidOperationException("Test failed");
        var compiledResult = new CompiledTestCaseResult(testCaseId, "TestGroup", exception);

        var executionSettingsTracking = new ExecutionSettingsTrackingFormat();
        var compiledResultTracking = new CompiledTestCaseResultTrackingFormat(
            "TestGroup",
            null, // no performance result for failed test
            exception,
            testCaseId);

        var classExecutionSummary = new ClassExecutionSummaryTrackingFormat(
            typeof(TestCompletionMessageMapperTests),
            executionSettingsTracking,
            new[] { compiledResultTracking });

        return new TestCaseCompletedNotification(
            classExecutionSummary,
            testInstanceContainer,
            new[] { testCase });
    }

    private void SetupMockDependencies()
    {
        // Setup console formatter to return a message for any IClassExecutionSummary array
        _consoleFormatter.FormConsoleWindowMessageForSailfish(Arg.Any<IClassExecutionSummary[]>())
            .Returns("Formatted console message");

        // Also setup for single IClassExecutionSummary (the actual call pattern)
        _consoleFormatter.FormConsoleWindowMessageForSailfish(Arg.Any<IEnumerable<IClassExecutionSummary>>())
            .Returns("Formatted console message");

        _runSettings.DisableAnalysisGlobally.Returns(false);
        _runSettings.RunScaleFish.Returns(true);
        _runSettings.RunSailDiff.Returns(true);

        _mediator.Send(Arg.Any<GetAllTrackingDataOrderedChronologicallyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetAllTrackingDataOrderedChronologicallyResponse(new TrackingFileDataList()));
    }

    private TestCaseCompletedNotification CreateTestNotificationWithNullPerformanceData()
    {
        var testCase = new TestCase("TestClass1.TestMethod1()", new Uri("executor://sailfish"), "TestAssembly.dll");
        var testCaseId = new TestCaseId("TestClass1.TestMethod1");

        // Create a real PerformanceTimer instance
        var performanceTimer = new PerformanceTimer();

        // Create a real TestInstanceContainerExternal
        var executionSettings = Substitute.For<IExecutionSettings>();
        var testInstanceContainer = new TestInstanceContainerExternal(
            typeof(TestCompletionMessageMapperTests),
            this,
            typeof(TestCompletionMessageMapperTests).GetMethod(nameof(CreateTestNotification))!,
            testCaseId,
            executionSettings,
            performanceTimer,
            false);

        var executionSettingsTracking = new ExecutionSettingsTrackingFormat();

        // Create compiled result with null performance data
        var compiledResultTracking = new CompiledTestCaseResultTrackingFormat(
            "TestGroup",
            null, // null performance result
            null,
            testCaseId);

        var classExecutionSummary = new ClassExecutionSummaryTrackingFormat(
            typeof(TestCompletionMessageMapperTests),
            executionSettingsTracking,
            new[] { compiledResultTracking });

        return new TestCaseCompletedNotification(
            classExecutionSummary,
            testInstanceContainer,
            new[] { testCase });
    }

    private TestCaseCompletedNotification CreateTestNotificationWithException()
    {
        var testCase = new TestCase("TestClass1.TestMethod1()", new Uri("executor://sailfish"), "TestAssembly.dll");
        var testCaseId = new TestCaseId("TestClass1.TestMethod1");

        // Create a real PerformanceTimer instance
        var performanceTimer = new PerformanceTimer();

        // Create a real TestInstanceContainerExternal
        var executionSettings = Substitute.For<IExecutionSettings>();
        var testInstanceContainer = new TestInstanceContainerExternal(
            typeof(TestCompletionMessageMapperTests),
            this,
            typeof(TestCompletionMessageMapperTests).GetMethod(nameof(CreateTestNotification))!,
            testCaseId,
            executionSettings,
            performanceTimer,
            false);

        var executionSettingsTracking = new ExecutionSettingsTrackingFormat();
        var exception = new InvalidOperationException("Test exception");

        // Create compiled result with exception
        var compiledResultTracking = new CompiledTestCaseResultTrackingFormat(
            "TestGroup",
            null, // no performance result for failed test
            exception,
            testCaseId);

        var classExecutionSummary = new ClassExecutionSummaryTrackingFormat(
            typeof(TestCompletionMessageMapperTests),
            executionSettingsTracking,
            new[] { compiledResultTracking });

        return new TestCaseCompletedNotification(
            classExecutionSummary,
            testInstanceContainer,
            new[] { testCase });
    }

    #endregion
}
