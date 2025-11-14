using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for TestCompletionQueuePublisher.
/// Tests message publishing, validation, and error handling scenarios.
/// </summary>
public class TestCompletionQueuePublisherTests : IDisposable
{
    private readonly ITestCompletionQueue _queue;
    private readonly ILogger _logger;
    private TestCompletionQueuePublisher? _publisher;

    public TestCompletionQueuePublisherTests()
    {
        _queue = Substitute.For<ITestCompletionQueue>();
        _logger = Substitute.For<ILogger>();
    }

    public void Dispose()
    {
        // TestCompletionQueuePublisher doesn't implement IDisposable
        // No cleanup needed for this test class
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullQueue_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionQueuePublisher(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionQueuePublisher(_queue, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);

        // Assert
        _publisher.ShouldNotBeNull();
    }

    #endregion

    #region PublishTestCompletion Tests

    [Fact]
    public async Task PublishTestCompletion_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => 
            _publisher.PublishTestCompletion(null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishTestCompletion_WithValidMessage_ShouldEnqueueMessage()
    {
        // Arrange
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);
        var message = CreateTestMessage();

        // Act
        await _publisher.PublishTestCompletion(message, CancellationToken.None);

        // Assert
        await _queue.Received(1).EnqueueAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishTestCompletion_WithCancellationToken_ShouldPassTokenToQueue()
    {
        // Arrange
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);
        var message = CreateTestMessage();
        using var cts = new CancellationTokenSource();

        // Act
        await _publisher.PublishTestCompletion(message, cts.Token);

        // Assert - Implementation passes cancellation token to queue but doesn't check it itself
        await _queue.Received(1).EnqueueAsync(message, cts.Token);
    }

    [Fact]
    public async Task PublishTestCompletion_WhenQueueThrows_ShouldPropagateException()
    {
        // Arrange
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);
        var message = CreateTestMessage();
        _queue.EnqueueAsync(Arg.Any<TestCompletionQueueMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Queue error")));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => 
            _publisher.PublishTestCompletion(message, CancellationToken.None));
    }

    [Fact]
    public async Task PublishTestCompletion_ShouldEnqueueMessage()
    {
        // Arrange
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);
        var message = CreateTestMessage();

        // Act
        await _publisher.PublishTestCompletion(message, CancellationToken.None);

        // Assert - Implementation doesn't log for successful operations, just enqueues
        await _queue.Received(1).EnqueueAsync(message, CancellationToken.None);
    }

    [Fact]
    public async Task PublishTestCompletion_WhenSuccessful_ShouldCompleteWithoutLogging()
    {
        // Arrange
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);
        var message = CreateTestMessage();

        // Act
        await _publisher.PublishTestCompletion(message, CancellationToken.None);

        // Assert - Implementation doesn't log success, just completes successfully
        await _queue.Received(1).EnqueueAsync(message, CancellationToken.None);
        _logger.DidNotReceive().Log(Arg.Any<LogLevel>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task PublishTestCompletion_WhenQueueFails_ShouldLogError()
    {
        // Arrange
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);
        var message = CreateTestMessage();
        var exception = new InvalidOperationException("Queue error");
        _queue.EnqueueAsync(Arg.Any<TestCompletionQueueMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => 
            _publisher.PublishTestCompletion(message, CancellationToken.None));

        _logger.Received().Log(LogLevel.Error, Arg.Is<string>(s => s.Contains("Failed to publish test completion message")));
    }

    [Fact]
    public async Task PublishTestCompletion_WithMultipleMessages_ShouldEnqueueAll()
    {
        // Arrange
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);
        var message1 = CreateTestMessage("TestCase1");
        var message2 = CreateTestMessage("TestCase2");
        var message3 = CreateTestMessage("TestCase3");

        // Act
        await _publisher.PublishTestCompletion(message1, CancellationToken.None);
        await _publisher.PublishTestCompletion(message2, CancellationToken.None);
        await _publisher.PublishTestCompletion(message3, CancellationToken.None);

        // Assert
        await _queue.Received(3).EnqueueAsync(Arg.Any<TestCompletionQueueMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishTestCompletion_WithLargeMessage_ShouldHandleCorrectly()
    {
        // Arrange
        _publisher = new TestCompletionQueuePublisher(_queue, _logger);
        var message = CreateLargeTestMessage();

        // Act & Assert - Should not throw
        await _publisher.PublishTestCompletion(message, CancellationToken.None);

        await _queue.Received(1).EnqueueAsync(message, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private TestCompletionQueueMessage CreateTestMessage(string testCaseId = "TestClass.TestMethod()")
    {
        return new TestCompletionQueueMessage
        {
            TestCaseId = testCaseId,
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

    private TestCompletionQueueMessage CreateLargeTestMessage()
    {
        // Create large metadata dictionary
        var largeMetadata = new Dictionary<string, object>();
        for (int i = 0; i < 1000; i++)
        {
            largeMetadata[$"Key{i}"] = $"Value{i}";
        }

        return new TestCompletionQueueMessage
        {
            TestCaseId = "TestClass.LargeTestMethod()",
            TestResult = new TestExecutionResult
            {
                IsSuccess = true,
                ExceptionMessage = null,
                ExceptionDetails = null
            },
            CompletedAt = DateTime.UtcNow,
            Metadata = largeMetadata,
            PerformanceMetrics = new PerformanceMetrics
            {
                MeanMs = 150.0,
                MedianMs = 145.0,
                StandardDeviation = 25.0
            }
        };
    }

    #endregion
}
