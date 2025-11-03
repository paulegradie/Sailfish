using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Attributes;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Logging;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.DefaultHandlers.Sailfish;

public class MethodComparisonTestRunCompletedHandlerTests
{
    private readonly ILogger mockLogger;
    private readonly IMediator mockMediator;
    private readonly MethodComparisonTestRunCompletedHandler handler;

    public MethodComparisonTestRunCompletedHandlerTests()
    {
        mockLogger = Substitute.For<ILogger>();
        mockMediator = Substitute.For<IMediator>();
        handler = new MethodComparisonTestRunCompletedHandler(mockLogger, mockMediator);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MethodComparisonTestRunCompletedHandler(null!, mockMediator));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMediatorIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MethodComparisonTestRunCompletedHandler(mockLogger, null!));
    }

    #endregion

    #region Handle Tests - Basic Functionality

    [Fact]
    public async Task Handle_WithValidNotification_CompletesSuccessfully()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToMarkdown();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<string>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithNoWriteToMarkdownAttribute_SkipsMarkdownGeneration()
    {
        // Arrange
        var notification = CreateTestNotificationWithoutWriteToMarkdown();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.DidNotReceive().Publish(
            Arg.Any<WriteMethodComparisonMarkdownNotification>(),
            Arg.Any<CancellationToken>());

        mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("No test classes found with WriteToMarkdown attribute")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithWriteToMarkdownAttribute_PublishesMarkdownNotification()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToMarkdown();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonMarkdownNotification>(n =>
                n.TestClassName.StartsWith("TestSession_") &&
                !string.IsNullOrEmpty(n.MarkdownContent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LogsClassCount()
    {
        // Arrange
        var notification = CreateTestNotificationWithMultipleClasses();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("TestRunCompletedNotification received")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Handle Tests - Markdown Content

    [Fact]
    public async Task Handle_WithComparisonMethods_GeneratesComparisonMatrix()
    {
        // Arrange
        var notification = CreateTestNotificationWithComparisonMethods();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonMarkdownNotification>(n =>
                n.MarkdownContent.Contains("Comparison Group:") &&
                n.MarkdownContent.Contains("Performance Comparison Matrix")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonComparisonMethods_GeneratesIndividualResults()
    {
        // Arrange
        var notification = CreateTestNotificationWithNonComparisonMethods();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonMarkdownNotification>(n =>
                n.MarkdownContent.Contains("Individual Test Results")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyResults_HandlesGracefully()
    {
        // Arrange
        var notification = CreateTestNotificationWithEmptyResults();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - Should not throw and should log appropriately
        mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<string>(),
            Arg.Any<object[]>());
    }

    #endregion

    #region Handle Tests - Exception Handling

    [Fact]
    public async Task Handle_WithException_LogsError()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToMarkdown();
        mockMediator.When(x => x.Publish(Arg.Any<WriteMethodComparisonMarkdownNotification>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("Test exception"));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockLogger.Received().Log(
            LogLevel.Error,
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("Failed to generate consolidated session markdown")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithMediatorException_DoesNotThrow()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToMarkdown();
        mockMediator.When(x => x.Publish(Arg.Any<WriteMethodComparisonMarkdownNotification>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("Mediator error"));

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await handler.Handle(notification, CancellationToken.None));
    }

    #endregion

    #region Handle Tests - Cancellation

    [Fact]
    public async Task Handle_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToMarkdown();
        var cts = new CancellationTokenSource();

        // Act
        await handler.Handle(notification, cts.Token);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Any<WriteMethodComparisonMarkdownNotification>(),
            cts.Token);
    }

    [Fact]
    public async Task Handle_WithCancelledToken_HandlesGracefully()
    {
        // Arrange
        var notification = CreateTestNotificationWithWriteToMarkdown();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockMediator.When(x => x.Publish(Arg.Any<WriteMethodComparisonMarkdownNotification>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new OperationCanceledException());

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await handler.Handle(notification, cts.Token));
    }

    #endregion

    #region Handle Tests - Multiple Classes

    [Fact]
    public async Task Handle_WithMultipleClassesWithWriteToMarkdown_ConsolidatesAllResults()
    {
        // Arrange
        var notification = CreateTestNotificationWithMultipleClassesWithWriteToMarkdown();

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Is<WriteMethodComparisonMarkdownNotification>(n =>
                !string.IsNullOrEmpty(n.MarkdownContent) &&
                n.TestClassName.StartsWith("TestSession_")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private TestRunCompletedNotification CreateTestNotificationWithWriteToMarkdown()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToMarkdown))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(10.5)
                    .WithMedian(10.0)
                    .WithSampleSize(100)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification(new[] { classExecutionSummary });
    }

    private TestRunCompletedNotification CreateTestNotificationWithoutWriteToMarkdown()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithoutWriteToMarkdown))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().Build()))
            .Build();

        return new TestRunCompletedNotification(new[] { classExecutionSummary });
    }

    private TestRunCompletedNotification CreateTestNotificationWithMultipleClasses()
    {
        var summary1 = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToMarkdown))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().Build()))
            .Build();

        var summary2 = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithoutWriteToMarkdown))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod2").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().Build()))
            .Build();

        return new TestRunCompletedNotification(new[] { summary1, summary2 });
    }

    private TestRunCompletedNotification CreateTestNotificationWithComparisonMethods()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithComparison))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Method1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(10.0)
                    .WithMedian(9.5)
                    .WithSampleSize(100)
                    .Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Method2").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(20.0)
                    .WithMedian(19.5)
                    .WithSampleSize(100)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification(new[] { classExecutionSummary });
    }

    private TestRunCompletedNotification CreateTestNotificationWithNonComparisonMethods()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToMarkdown))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("RegularMethod").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(15.0)
                    .WithMedian(14.5)
                    .WithSampleSize(100)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification(new[] { classExecutionSummary });
    }

    private TestRunCompletedNotification CreateTestNotificationWithEmptyResults()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToMarkdown))
            .Build();

        return new TestRunCompletedNotification(new[] { classExecutionSummary });
    }

    private TestRunCompletedNotification CreateTestNotificationWithMultipleClassesWithWriteToMarkdown()
    {
        var summary1 = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClassWithWriteToMarkdown))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(10.0)
                    .Build()))
            .Build();

        var summary2 = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(AnotherTestClassWithWriteToMarkdown))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("TestMethod2").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(20.0)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification(new[] { summary1, summary2 });
    }

    #endregion

    #region Test Classes

    [WriteToMarkdown]
    private class TestClassWithWriteToMarkdown
    {
        public void TestMethod1() { }
        public void RegularMethod() { }
    }

    private class TestClassWithoutWriteToMarkdown
    {
        public void TestMethod1() { }
    }

    [WriteToMarkdown]
    private class TestClassWithComparison
    {
        [SailfishComparison("TestGroup")]
        public void Method1() { }

        [SailfishComparison("TestGroup")]
        public void Method2() { }
    }

    [WriteToMarkdown]
    private class AnotherTestClassWithWriteToMarkdown
    {
        public void TestMethod2() { }
    }

    #endregion
}

