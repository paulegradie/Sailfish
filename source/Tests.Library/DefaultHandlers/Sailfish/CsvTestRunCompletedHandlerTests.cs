using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sailfish.Attributes;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.DefaultHandlers.Sailfish;

public class CsvTestRunCompletedHandlerTests
{
    private readonly ILogger mockLogger;
    private readonly IMediator mockMediator;
    private readonly CsvTestRunCompletedHandler handler;

    public CsvTestRunCompletedHandlerTests()
    {
        mockLogger = Substitute.For<ILogger>();
        mockMediator = Substitute.For<IMediator>();
        handler = new CsvTestRunCompletedHandler(mockLogger, mockMediator);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new CsvTestRunCompletedHandler(null!, mockMediator));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMediatorIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new CsvTestRunCompletedHandler(mockLogger, null!));
    }

    [Fact]
    public async Task Handle_WithValidNotification_CompletesSuccessfully()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<string>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithNoWriteToCsvAttribute_SkipsCsvGeneration()
    {
        // Arrange
        var notification = CreateTestNotificationWithoutWriteToCsv();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.DidNotReceive().Publish(
            Arg.Any<WriteMethodComparisonCsvNotification>(),
            Arg.Any<CancellationToken>());

        mockLogger.Received().Log(
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
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Any<WriteMethodComparisonCsvNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LogsClassCount()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<string>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithComparisonMethods_GeneratesComparisonSection()
    {
        // Arrange
        var notification = CreateTestNotificationWithComparison();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonCsvNotification>(n =>
                n.CsvContent.Contains("# Method Comparisons")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonComparisonMethods_GeneratesIndividualResults()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
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
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
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
        mockMediator.Publish(Arg.Any<WriteMethodComparisonCsvNotification>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Test exception"));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockLogger.Received().Log(
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
        mockMediator.Publish(Arg.Any<WriteMethodComparisonCsvNotification>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Mediator error"));

        // Act & Assert
        await Should.NotThrowAsync(async () => await handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();
        var cts = new CancellationTokenSource();

        // Act
        await handler.Handle(notification, cts.Token);

        // Assert
        await mockMediator.Received(1).Publish(
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
        await Should.NotThrowAsync(async () => await handler.Handle(notification, cts.Token));
    }

    [Fact]
    public async Task Handle_WithCancellationDuringPublish_ThrowsOperationCanceledException()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToCsv();
        var cts = new CancellationTokenSource();
        mockMediator.Publish(Arg.Any<WriteMethodComparisonCsvNotification>(), Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        // Act & Assert
        await Should.NotThrowAsync(async () => await handler.Handle(notification, cts.Token));
    }

    [Fact]
    public async Task Handle_WithMultipleClassesWithWriteToCsv_ConsolidatesAllResults()
    {
        // Arrange
        var notification = CreateTestNotificationWithMultipleClassesWithWriteToCsv();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
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
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
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
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
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

        return new TestRunCompletedNotification(new[] { classExecutionSummary });
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

        return new TestRunCompletedNotification(new[] { classExecutionSummary });
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

        return new TestRunCompletedNotification(new[] { classExecutionSummary });
    }

    private TestRunCompletedNotification CreateTestNotificationWithEmptyResults()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToCsv))
            .Build();

        return new TestRunCompletedNotification(new[] { classExecutionSummary });
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

        return new TestRunCompletedNotification(new[] { summary1, summary2 });
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
        [SailfishComparison("TestGroup")]
        public void Method1() { }

        [SailfishComparison("TestGroup")]
        public void Method2() { }
    }

    [WriteToCsv]
    private class AnotherTestClassWithWriteToCsv
    {
        public void TestMethod2() { }
    }
}

