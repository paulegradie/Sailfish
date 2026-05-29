using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish.Attributes;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Logging;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.DefaultHandlers.Sailfish;

public class CsvTestRunCompletedHandlerTests
{
    private readonly ILogger _mockLogger;
    private readonly IMediator _mockMediator;
    private readonly CsvTestRunCompletedHandler _handler;

    public CsvTestRunCompletedHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger>();
        _mockMediator = Substitute.For<IMediator>();
        _handler = new CsvTestRunCompletedHandler(_mockLogger, _mockMediator);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new CsvTestRunCompletedHandler(null!, _mockMediator));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMediatorIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new CsvTestRunCompletedHandler(_mockLogger, null!));
    }

    [Fact]
    public async Task Handle_WithValidNotification_CompletesSuccessfully()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Any<WriteMethodComparisonCsvNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoWriteToCsvAttribute_SkipsCsvGeneration()
    {
        // Arrange
        var notification = CreateTestNotificationWithoutWriteToCsv();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.DidNotReceive().Publish(
            Arg.Any<WriteMethodComparisonCsvNotification>(),
            Arg.Any<CancellationToken>());

        _mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("No test classes found with WriteToCsv attribute")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithWriteToCsvAttribute_PublishesCsvNotification()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Any<WriteMethodComparisonCsvNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LogsClassCount()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("class", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithComparisonMethods_GeneratesComparisonSection()
    {
        // Arrange
        var notification = CreateTestNotificationWithComparison();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                n.CsvContent.Contains("# Method Comparisons")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBaselineComparison_EmitsOnlyBaselineVsContenderRows()
    {
        // Arrange — a 2-method group with one baseline → exactly one CSV pair row should appear.
        var notification = CreateTestNotificationWithBaselineComparison();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                n.CsvContent.Contains("# Method Comparisons") &&
                n.CsvContent.Contains("TestGroup,Baseline,Contender")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSameGroupNameInTwoClasses_EmitsBaselineRowsForEach()
    {
        // Regression: per-class grouping must keep each class's lone baseline intact.
        // If grouping were by group-name only, the merged group would see 2 baselines and
        // collapse into N×N pair rows (Baseline,Contender,OtherBaseline,OtherContender pairs).
        var notification = CreateTestNotificationWithSameGroupNameInTwoClasses();

        await _handler.Handle(notification, CancellationToken.None);

        await _mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                n.CsvContent.Contains("# Method Comparisons") &&
                // Each class emits exactly its own baseline-vs-contender row.
                n.CsvContent.Contains("TestGroup,Baseline,Contender") &&
                n.CsvContent.Contains("TestGroup,OtherBaseline,OtherContender") &&
                // The cross-class N×N pair must NOT appear.
                !n.CsvContent.Contains("TestGroup,Baseline,OtherBaseline") &&
                !n.CsvContent.Contains("TestGroup,Contender,OtherContender")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithLegacyComparisonAttribute_StillEmitsComparison()
    {
        // Arrange — exercises the obsolete-attribute fallback path.
        var notification = CreateTestNotificationWithLegacyComparison();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                n.CsvContent.Contains("# Method Comparisons") &&
                n.CsvContent.Contains("TestGroup,")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonComparisonMethods_GeneratesIndividualResults()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                n.CsvContent.Contains("# Individual Test Results")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyResults_HandlesGracefully()
    {
        // Arrange
        var notification = CreateTestNotificationWithEmptyResults();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                !string.IsNullOrEmpty(n.CsvContent) &&
                n.CsvContent.Contains("# Session Metadata")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithException_LogsError()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();
        _mockMediator.Publish(Arg.Any<WriteMethodComparisonCsvNotification>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Test exception"));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Error,
            Arg.Any<Exception>(),
            Arg.Any<string>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithMediatorException_DoesNotThrow()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();
        _mockMediator.Publish(Arg.Any<WriteMethodComparisonCsvNotification>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Mediator error"));

        // Act & Assert
        await Should.NotThrowAsync(async () => await _handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();
        var cts = new CancellationTokenSource();

        // Act
        await _handler.Handle(notification, cts.Token);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Any<WriteMethodComparisonCsvNotification>(),
            cts.Token);
    }

    [Fact]
    public async Task Handle_WithCancelledToken_HandlesGracefully()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.NotThrowAsync(async () => await _handler.Handle(notification, cts.Token));
    }

    [Fact]
    public async Task Handle_WithCancellationDuringPublish_DoesNotThrow()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();
        var cts = new CancellationTokenSource();
        _mockMediator.Publish(Arg.Any<WriteMethodComparisonCsvNotification>(), Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        // Act & Assert
        await Should.NotThrowAsync(async () => await _handler.Handle(notification, cts.Token));
    }

    [Fact]
    public async Task Handle_WithMultipleClassesWithWriteToCsv_ConsolidatesAllResults()
    {
        // Arrange
        var notification = CreateTestNotificationWithMultipleClassesWithWriteToCsv();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                !string.IsNullOrEmpty(n.CsvContent) &&
                n.TestClassName.StartsWith("TestSession_")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GeneratesCsvWithSessionMetadata()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                n.CsvContent.Contains("# Session Metadata") &&
                n.CsvContent.Contains("SessionId,Timestamp,TotalClasses,TotalTests")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GeneratesCsvWithPerformanceMetrics()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                n.CsvContent.Contains("TestClass,TestMethod,MeanTime,MedianTime,StdDev,SampleSize,ComparisonGroup,Status")),
            Arg.Any<CancellationToken>());
    }

    // Helper methods to create test notifications
    private TestRunCompletedNotification CreateTestNotificationWithWriteToCsv()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToCsv))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(100.0)
                    .WithMedian(95.0)
                    .WithStdDev(5.0)
                    .WithSampleSize(10)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification([classExecutionSummary]);
    }

    private TestRunCompletedNotification CreateTestNotificationWithoutWriteToCsv()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithoutWriteToCsv))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(100.0)
                    .WithMedian(95.0)
                    .WithStdDev(5.0)
                    .WithSampleSize(10)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification([classExecutionSummary]);
    }

    private TestRunCompletedNotification CreateTestNotificationWithComparison()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithComparison))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Method1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(100.0)
                    .WithMedian(95.0)
                    .WithStdDev(5.0)
                    .WithSampleSize(10)
                    .Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Method2").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(200.0)
                    .WithMedian(195.0)
                    .WithStdDev(10.0)
                    .WithSampleSize(10)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification([classExecutionSummary]);
    }

    private TestRunCompletedNotification CreateTestNotificationWithBaselineComparison()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithBaselineComparison))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Baseline").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(100.0)
                    .WithMedian(95.0)
                    .WithStdDev(5.0)
                    .WithSampleSize(10)
                    .Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Contender").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(200.0)
                    .WithMedian(195.0)
                    .WithStdDev(10.0)
                    .WithSampleSize(10)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification([classExecutionSummary]);
    }

    private TestRunCompletedNotification CreateTestNotificationWithSameGroupNameInTwoClasses()
    {
        var classA = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithBaselineComparison))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Baseline").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(100.0).WithMedian(95.0).WithStdDev(5.0).WithSampleSize(10).Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Contender").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(200.0).WithMedian(195.0).WithStdDev(10.0).WithSampleSize(10).Build()))
            .Build();

        var classB = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(OtherTestClassWithBaselineComparison))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("OtherBaseline").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(150.0).WithMedian(145.0).WithStdDev(7.0).WithSampleSize(10).Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("OtherContender").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(300.0).WithMedian(295.0).WithStdDev(15.0).WithSampleSize(10).Build()))
            .Build();

        return new TestRunCompletedNotification([classA, classB]);
    }

    private TestRunCompletedNotification CreateTestNotificationWithLegacyComparison()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(LegacyTestClassWithComparison))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Method1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(100.0)
                    .WithMedian(95.0)
                    .WithStdDev(5.0)
                    .WithSampleSize(10)
                    .Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Method2").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(200.0)
                    .WithMedian(195.0)
                    .WithStdDev(10.0)
                    .WithSampleSize(10)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification([classExecutionSummary]);
    }

    private TestRunCompletedNotification CreateTestNotificationWithEmptyResults()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToCsv))
            .Build();

        return new TestRunCompletedNotification([classExecutionSummary]);
    }

    private TestRunCompletedNotification CreateTestNotificationWithMultipleClassesWithWriteToCsv()
    {
        var summary1 = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToCsv))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(100.0)
                    .WithMedian(95.0)
                    .WithStdDev(5.0)
                    .WithSampleSize(10)
                    .Build()))
            .Build();

        var summary2 = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(AnotherTestClassWithWriteToCsv))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod2").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(150.0)
                    .WithMedian(145.0)
                    .WithStdDev(7.0)
                    .WithSampleSize(10)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification([summary1, summary2]);
    }

    // Test classes for attribute testing
    [WriteToCsv]
    private class TestClassWithWriteToCsv
    {
        public void TestMethod1() { }
        public void RegularMethod() { }
    }

    private class TestClassWithoutWriteToCsv
    {
        public void TestMethod1() { }
    }

    [WriteToCsv]
    private class TestClassWithComparison
    {
        [SailfishMethod(ComparisonGroup = "TestGroup")]
        public void Method1() { }

        [SailfishMethod(ComparisonGroup = "TestGroup")]
        public void Method2() { }
    }

    [WriteToCsv]
    private class TestClassWithBaselineComparison
    {
        [SailfishMethod(ComparisonGroup = "TestGroup", IsBaseline = true)]
        public void Baseline() { }

        [SailfishMethod(ComparisonGroup = "TestGroup")]
        public void Contender() { }
    }

    // Deliberately reuses the "TestGroup" name from TestClassWithBaselineComparison to verify
    // per-class scoping.
    [WriteToCsv]
    private class OtherTestClassWithBaselineComparison
    {
        [SailfishMethod(ComparisonGroup = "TestGroup", IsBaseline = true)]
        public void OtherBaseline() { }

        [SailfishMethod(ComparisonGroup = "TestGroup")]
        public void OtherContender() { }
    }

    [WriteToCsv]
    private class LegacyTestClassWithComparison
    {
        // Coverage for the deprecation-window fallback: legacy [SailfishComparison] still resolves.
#pragma warning disable CS0618 // Type or member is obsolete
        [SailfishComparison("TestGroup")]
        public void Method1() { }

        [SailfishComparison("TestGroup")]
        public void Method2() { }
#pragma warning restore CS0618
    }

    [WriteToCsv]
    private class AnotherTestClassWithWriteToCsv
    {
        public void TestMethod2() { }
    }
}

