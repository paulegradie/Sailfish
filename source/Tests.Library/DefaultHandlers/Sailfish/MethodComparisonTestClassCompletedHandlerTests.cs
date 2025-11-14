using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Tests.Common.Builders;
using Tests.Common.Utils;
using Xunit;

namespace Tests.Library.DefaultHandlers.Sailfish;

public class MethodComparisonTestClassCompletedHandlerTests
{
    private readonly ILogger _mockLogger;
    private readonly IMediator _mockMediator;
    private readonly MethodComparisonTestClassCompletedHandler _handler;

    public MethodComparisonTestClassCompletedHandlerTests()
    {
        _mockLogger = Substitute.For<ILogger>();
        _mockMediator = Substitute.For<IMediator>();
        _handler = new MethodComparisonTestClassCompletedHandler(_mockLogger, _mockMediator);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MethodComparisonTestClassCompletedHandler(null!, _mockMediator));
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MethodComparisonTestClassCompletedHandler(_mockLogger, null!));
    }

    [Fact]
    public async Task Handle_WithValidNotification_LogsDebugMessages()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("TestClassCompletedNotification received")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithValidNotification_SkipsProcessingDueToDisabledHandler()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Handler is disabled, so no markdown notification should be published
        await _mockMediator.DidNotReceive().Publish(
            Arg.Any<WriteMethodComparisonMarkdownNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidNotification_LogsSkipMessage()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("Skipping per-class markdown generation")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithNullTestClass_DoesNotThrow()
    {
        // Arrange
        var notification = CreateTestNotificationWithNullTestClass();

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await _handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithEmptyTestResults_DoesNotThrow()
    {
        // Arrange
        var notification = CreateTestNotificationWithEmptyResults();

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await _handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithMultipleTestResults_LogsCorrectly()
    {
        // Arrange
        var notification = CreateTestNotificationWithMultipleResults();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<string>(),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var notification = CreateTestNotification();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Should not throw even with cancelled token since handler returns early
        await Should.NotThrowAsync(async () =>
            await _handler.Handle(notification, cts.Token));
    }

    [Fact]
    public async Task Handle_WithExceptionInLogging_DoesNotThrow()
    {
        // Arrange
        var notification = CreateTestNotification();
        _mockLogger.When(x => x.Log(Arg.Any<LogLevel>(), Arg.Any<string>(), Arg.Any<object[]>()))
            .Do(_ => throw new InvalidOperationException("Logging failed"));

        // Act & Assert - Handler should catch and handle exceptions
        await Should.NotThrowAsync(async () =>
            await _handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithTestResultsContainingNullPerformanceResults_DoesNotThrow()
    {
        // Arrange
        var notification = CreateTestNotificationWithNullPerformanceResults();

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await _handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithTestResultsContainingExceptions_DoesNotThrow()
    {
        // Arrange
        var notification = CreateTestNotificationWithExceptions();

        // Act & Assert
        await Should.NotThrowAsync(async () =>
            await _handler.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_LogsTestClassName()
    {
        // Arrange
        var notification = CreateTestNotification();

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - Verify that logging was called with Debug level
        _mockLogger.Received().Log(
            LogLevel.Debug,
            Arg.Any<string>(),
            Arg.Any<object[]>());
    }

    // Helper methods to create test notifications

    private TestClassCompletedNotification CreateTestNotification()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClass))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().Build()))
            .Build();

        return new TestClassCompletedNotification(
            classExecutionSummary,
            CreateTestInstanceContainer(),
            new List<dynamic>());
    }

    private TestClassCompletedNotification CreateTestNotificationWithNullTestClass()
    {
        var classExecutionSummary = new ClassExecutionSummaryTrackingFormat(
            null!,
            ExecutionSettingsTrackingFormatBuilder.Create().Build(),
            new List<CompiledTestCaseResultTrackingFormat>());

        return new TestClassCompletedNotification(
            classExecutionSummary,
            CreateTestInstanceContainer(),
            new List<dynamic>());
    }

    private TestClassCompletedNotification CreateTestNotificationWithEmptyResults()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClass))
            .WithCompiledTestCaseResult([])
            .Build();

        return new TestClassCompletedNotification(
            classExecutionSummary,
            CreateTestInstanceContainer(),
            new List<dynamic>());
    }

    private TestClassCompletedNotification CreateTestNotificationWithMultipleResults()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClass))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Test1").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Test2").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().Build()))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Test3").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().Build()))
            .Build();

        return new TestClassCompletedNotification(
            classExecutionSummary,
            CreateTestInstanceContainer(),
            new List<dynamic>());
    }

    private TestClassCompletedNotification CreateTestNotificationWithNullPerformanceResults()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClass))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().Build())
                .WithPerformanceRunResult(null))
            .Build();

        return new TestClassCompletedNotification(
            classExecutionSummary,
            CreateTestInstanceContainer(),
            new List<dynamic>());
    }

    private TestClassCompletedNotification CreateTestNotificationWithExceptions()
    {
        var classExecutionSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestClass))
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().Build())
                .WithException(new InvalidOperationException("Test exception"))
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create().Build()))
            .Build();

        return new TestClassCompletedNotification(
            classExecutionSummary,
            CreateTestInstanceContainer(),
            new List<dynamic>());
    }

    private TestInstanceContainerExternal CreateTestInstanceContainer()
    {
        return new TestInstanceContainerExternal(
            typeof(TestClass),
            new TestClass(),
            typeof(TestClass).GetMethod(nameof(TestClass.TestMethod))!,
            TestCaseIdBuilder.Create().Build(),
            Substitute.For<IExecutionSettings>(),
            new PerformanceTimer(),
            false);
    }

    private class TestClass
    {
        public void TestMethod()
        {
        }
    }
}

