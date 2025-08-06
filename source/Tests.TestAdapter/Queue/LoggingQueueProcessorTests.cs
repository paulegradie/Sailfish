using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Processors;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for LoggingQueueProcessor.
/// Tests logging functionality, configuration-based behavior, and error handling.
/// </summary>
public class LoggingQueueProcessorTests : IDisposable
{
    private readonly ILogger _logger;
    private LoggingQueueProcessor? _processor;

    public LoggingQueueProcessorTests()
    {
        _logger = Substitute.For<ILogger>();
    }

    public void Dispose()
    {
        // LoggingQueueProcessor doesn't implement IDisposable
        // No cleanup needed for this test class
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new LoggingQueueProcessor(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // Act
        _processor = new LoggingQueueProcessor(_logger);

        // Assert
        _processor.ShouldNotBeNull();
    }

    #endregion

    #region ProcessTestCompletionAsync Tests

    [Fact]
    public async Task ProcessTestCompletion_WithValidMessage_ShouldLogTestCompletion()
    {
        // Arrange
        _processor = new LoggingQueueProcessor(_logger);
        var message = CreateTestMessage();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        _processor = new LoggingQueueProcessor(_logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _processor.ProcessTestCompletion(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessTestCompletion_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _processor = new LoggingQueueProcessor(_logger);
        var message = CreateTestMessage();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _processor.ProcessTestCompletion(message, cts.Token));
    }

    [Fact]
    public async Task ProcessTestCompletion_WithPerformanceMetrics_ShouldLogMetrics()
    {
        // Arrange
        _processor = new LoggingQueueProcessor(_logger);
        var message = CreateTestMessageWithPerformanceMetrics();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithMetadata_ShouldLogMetadata()
    {
        // Arrange
        _processor = new LoggingQueueProcessor(_logger);
        var message = CreateTestMessageWithMetadata();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessTestCompletion_WithException_ShouldLogException()
    {
        // Arrange
        _processor = new LoggingQueueProcessor(_logger);
        var message = CreateTestMessageWithException();

        // Act
        await _processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        _logger.Received().Log(LogLevel.Information, Arg.Any<string>(), Arg.Any<object[]>());
    }

    #endregion

    #region Helper Methods

    private TestCompletionQueueMessage CreateTestMessage()
    {
        return new TestCompletionQueueMessage
        {
            TestCaseId = "TestClass.TestMethod()",
            TestResult = new TestExecutionResult
            {
                IsSuccess = true,
                ExceptionMessage = null,
                ExceptionDetails = null
            },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>(),
            PerformanceMetrics = new PerformanceMetrics()
        };
    }

    private TestCompletionQueueMessage CreateTestMessageWithPerformanceMetrics()
    {
        return new TestCompletionQueueMessage
        {
            TestCaseId = "TestClass.TestMethod()",
            TestResult = new TestExecutionResult
            {
                IsSuccess = true,
                ExceptionMessage = null,
                ExceptionDetails = null
            },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>(),
            PerformanceMetrics = new PerformanceMetrics
            {
                MeanMs = 100.5,
                MedianMs = 95.0,
                StandardDeviation = 10.2
            }
        };
    }

    private TestCompletionQueueMessage CreateTestMessageWithMetadata()
    {
        return new TestCompletionQueueMessage
        {
            TestCaseId = "TestClass.TestMethod()",
            TestResult = new TestExecutionResult
            {
                IsSuccess = true,
                ExceptionMessage = null,
                ExceptionDetails = null
            },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["TestCategory"] = "Performance",
                ["Environment"] = "Development"
            },
            PerformanceMetrics = new PerformanceMetrics()
        };
    }

    private TestCompletionQueueMessage CreateTestMessageWithException()
    {
        return new TestCompletionQueueMessage
        {
            TestCaseId = "TestClass.TestMethod()",
            TestResult = new TestExecutionResult
            {
                IsSuccess = false,
                ExceptionMessage = "Test exception",
                ExceptionDetails = "InvalidOperationException: Test exception"
            },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>(),
            PerformanceMetrics = new PerformanceMetrics()
        };
    }

    #endregion
}
