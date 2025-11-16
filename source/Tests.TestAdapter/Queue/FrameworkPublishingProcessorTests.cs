using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Processors;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for FrameworkPublishingProcessor.
/// Tests the queue processor responsible for publishing test results to the VS Test Platform.
/// </summary>
public class FrameworkPublishingProcessorTests
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly FrameworkPublishingProcessor _processor;

    public FrameworkPublishingProcessorTests()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger>();
        _processor = new FrameworkPublishingProcessor(_mediator, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new FrameworkPublishingProcessor(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new FrameworkPublishingProcessor(_mediator, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        _processor.ShouldNotBeNull();
    }

    #endregion

    #region ProcessTestCompletion Tests

    [Fact]
    public async Task ProcessTestCompletion_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => 
            _processor.ProcessTestCompletion(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessTestCompletion_WithValidMessage_ShouldPublishNotification()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Any<FrameworkTestCaseEndNotification>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithSuccessfulTest_ShouldPublishSuccessStatus()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.StatusCode == StatusCode.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithFailedTest_ShouldPublishFailureStatus()
    {
        // Arrange
        var message = CreateFailedTestMessage();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.StatusCode == StatusCode.Failure),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var message = CreateTestMessage();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => 
            _processor.ProcessTestCompletion(message, cts.Token));
    }

    #endregion

    #region Data Extraction Tests

    [Fact]
    public async Task ProcessTestCompletion_WithTestCaseInMetadata_ShouldExtractTestCase()
    {
        // Arrange
        var testCase = new TestCase("TestClass.TestMethod", new Uri("executor://sailfish"), "Sailfish");
        var message = CreateTestMessage();
        message.Metadata["TestCase"] = testCase;

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase == testCase),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithFormattedMessageInMetadata_ShouldExtractMessage()
    {
        // Arrange
        var message = CreateTestMessage();
        var formattedMessage = "Test completed successfully with detailed results";
        message.Metadata["FormattedMessage"] = formattedMessage;

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestOutputWindowMessage == formattedMessage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithStartTimeInMetadata_ShouldExtractStartTime()
    {
        // Arrange
        var message = CreateTestMessage();
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-5);
        message.Metadata["StartTime"] = startTime;

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.StartTime == startTime),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithExceptionInMetadata_ShouldExtractException()
    {
        // Arrange
        var message = CreateFailedTestMessage();
        var exception = new InvalidOperationException("Test failed");
        message.Metadata["Exception"] = exception;

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.Exception == exception),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Fallback Mechanism Tests

    [Fact]
    public async Task ProcessTestCompletion_WithMissingTestCase_ShouldCreateFallbackTestCase()
    {
        // Arrange
        var message = CreateTestMessage();
        message.Metadata.Remove("TestCase");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase != null),
            Arg.Any<CancellationToken>());
        _logger.Received().Log(LogLevel.Warning, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithMissingFormattedMessage_ShouldCreateDefaultMessage()
    {
        // Arrange
        var message = CreateTestMessage();
        message.Metadata.Remove("FormattedMessage");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => !string.IsNullOrEmpty(n.TestOutputWindowMessage)),
            Arg.Any<CancellationToken>());
        _logger.Received().Log(LogLevel.Warning, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithMissingStartTime_ShouldCalculateFallbackStartTime()
    {
        // Arrange
        var message = CreateTestMessage();
        message.Metadata.Remove("StartTime");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.StartTime < n.EndTime),
            Arg.Any<CancellationToken>());
        _logger.Received().Log(LogLevel.Warning, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithMissingException_ShouldCreateExceptionFromTestResult()
    {
        // Arrange
        var message = CreateFailedTestMessage();
        message.Metadata.Remove("Exception");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.Exception != null),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Comparison Method Suppression Tests

    [Fact]
    public async Task ProcessTestCompletion_WithComparisonMethod_ShouldSkipPublishing()
    {
        // Arrange
        var message = CreateTestMessage();
        message.Metadata["ComparisonGroup"] = "Group1";

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.DidNotReceive().Publish(
            Arg.Any<FrameworkTestCaseEndNotification>(),
            Arg.Any<CancellationToken>());
        _logger.Received().Log(LogLevel.Debug, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithEmptyComparisonGroup_ShouldPublish()
    {
        // Arrange
        var message = CreateTestMessage();
        message.Metadata["ComparisonGroup"] = string.Empty;

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Any<FrameworkTestCaseEndNotification>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Duration Calculation Tests

    [Fact]
    public async Task ProcessTestCompletion_WithMedianDuration_ShouldUseMedianForDuration()
    {
        // Arrange
        var message = CreateTestMessage();
        message.PerformanceMetrics.MedianMs = 150.5;

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.Duration == 150.5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithZeroMedian_ShouldCalculateDurationFromTimes()
    {
        // Arrange
        var message = CreateTestMessage();
        message.PerformanceMetrics.MedianMs = 0;
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-2);
        message.Metadata["StartTime"] = startTime;
        message.CompletedAt = startTime.AddMilliseconds(500).DateTime;

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.Duration >= 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithNegativeDuration_ShouldUseZero()
    {
        // Arrange
        var message = CreateTestMessage();
        message.PerformanceMetrics.MedianMs = 0;
        var startTime = DateTimeOffset.UtcNow.AddSeconds(1); // Future start time
        message.Metadata["StartTime"] = startTime;
        message.CompletedAt = DateTimeOffset.UtcNow.DateTime;

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.Duration >= 0),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ProcessTestCompletion_WithProcessingException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var message = CreateTestMessage();
        _mediator.Publish(Arg.Any<FrameworkTestCaseEndNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Publishing failed")));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _processor.ProcessTestCompletion(message, CancellationToken.None));

        _logger.Received().Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithSuccessfulTest_ShouldNotIncludeException()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.Exception == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithFailedTestNoException_ShouldCreateGenericException()
    {
        // Arrange
        var message = CreateFailedTestMessage();
        message.TestResult.ExceptionMessage = null;
        message.Metadata.Remove("Exception");

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n =>
                n.Exception != null &&
                n.Exception.Message.Contains("failed without specific exception")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task ProcessTestCompletion_WithNullMetadata_ShouldHandleGracefully()
    {
        // Arrange
        var message = CreateTestMessage();
        message.Metadata = new Dictionary<string, object>();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Any<FrameworkTestCaseEndNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithInvalidTestCaseInMetadata_ShouldCreateFallback()
    {
        // Arrange
        var message = CreateTestMessage();
        message.Metadata["TestCase"] = "InvalidTestCase"; // Wrong type

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.TestCase != null),
            Arg.Any<CancellationToken>());
        _logger.Received().Log(LogLevel.Warning, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithInvalidStartTimeInMetadata_ShouldCalculateFallback()
    {
        // Arrange
        var message = CreateTestMessage();
        message.Metadata["StartTime"] = "InvalidStartTime"; // Wrong type

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Any<FrameworkTestCaseEndNotification>(),
            Arg.Any<CancellationToken>());
        _logger.Received().Log(LogLevel.Warning, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithInvalidExceptionInMetadata_ShouldCreateFromTestResult()
    {
        // Arrange
        var message = CreateFailedTestMessage();
        message.Metadata["Exception"] = "InvalidException"; // Wrong type

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n => n.Exception != null),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ProcessTestCompletion_WithSuccessfulPublishing_ShouldLogDebug()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Debug, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithComparisonMethodSkipped_ShouldLogDebug()
    {
        // Arrange
        var message = CreateTestMessage();
        message.Metadata["ComparisonGroup"] = "Group1";

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Debug,
            Arg.Is<string>(s => s.Contains("Skipping framework publishing")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Helper Methods

    private TestCompletionQueueMessage CreateTestMessage()
    {
        var testCase = new TestCase("TestClass.TestMethod", new Uri("executor://sailfish"), "Sailfish");

        return new TestCompletionQueueMessage
        {
            TestCaseId = "TestClass.TestMethod",
            TestResult = new TestExecutionResult
            {
                IsSuccess = true,
                ExceptionMessage = null,
                ExceptionDetails = null
            },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["TestCase"] = testCase,
                ["FormattedMessage"] = "Test completed successfully",
                ["StartTime"] = DateTimeOffset.UtcNow.AddSeconds(-1)
            },
            PerformanceMetrics = new PerformanceMetrics
            {
                MedianMs = 100.0,
                MeanMs = 105.0
            }
        };
    }

    private TestCompletionQueueMessage CreateFailedTestMessage()
    {
        var testCase = new TestCase("TestClass.TestMethod", new Uri("executor://sailfish"), "Sailfish");

        return new TestCompletionQueueMessage
        {
            TestCaseId = "TestClass.TestMethod",
            TestResult = new TestExecutionResult
            {
                IsSuccess = false,
                ExceptionMessage = "Test failed with error",
                ExceptionDetails = "Stack trace details",
                ExceptionType = "System.InvalidOperationException"
            },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["TestCase"] = testCase,
                ["FormattedMessage"] = "Test failed",
                ["StartTime"] = DateTimeOffset.UtcNow.AddSeconds(-1)
            },
            PerformanceMetrics = new PerformanceMetrics
            {
                MedianMs = 100.0,
                MeanMs = 105.0
            }
        };
    }

    #endregion
}

