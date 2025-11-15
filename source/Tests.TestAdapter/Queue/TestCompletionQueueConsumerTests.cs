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
/// Comprehensive unit tests for TestCompletionQueueConsumer.
/// Tests processor registration, message processing, error handling, and lifecycle management.
/// </summary>
public class TestCompletionQueueConsumerTests : IDisposable
{
    private readonly ITestCompletionQueue _queue;
    private readonly ILogger _logger;
    private readonly IProcessingMetricsCollector _metricsCollector;
    private TestCompletionQueueConsumer? _consumer;

    public TestCompletionQueueConsumerTests()
    {
        _queue = Substitute.For<ITestCompletionQueue>();
        _logger = Substitute.For<ILogger>();
        _metricsCollector = Substitute.For<IProcessingMetricsCollector>();
    }

    public void Dispose()
    {
        _consumer?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullQueue_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionQueueConsumer(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCompletionQueueConsumer(_queue, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);

        // Assert
        _consumer.ShouldNotBeNull();
        _consumer.IsRunning.ShouldBeFalse();
        _consumer.ProcessorCount.ShouldBe(0);
    }

    [Fact]
    public void Constructor_WithMetricsCollector_ShouldCreateInstance()
    {
        // Act
        _consumer = new TestCompletionQueueConsumer(_queue, _logger, _metricsCollector);

        // Assert
        _consumer.ShouldNotBeNull();
    }

    #endregion

    #region RegisterProcessor Tests

    [Fact]
    public void RegisterProcessor_WithNullProcessor_ShouldThrowArgumentNullException()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _consumer.RegisterProcessor(null!));
    }

    [Fact]
    public void RegisterProcessor_WithValidProcessor_ShouldIncreaseProcessorCount()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        var processor = Substitute.For<ITestCompletionQueueProcessor>();

        // Act
        _consumer.RegisterProcessor(processor);

        // Assert
        _consumer.ProcessorCount.ShouldBe(1);
    }

    [Fact]
    public void RegisterProcessor_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        _consumer.Dispose();
        var processor = Substitute.For<ITestCompletionQueueProcessor>();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => _consumer.RegisterProcessor(processor));
    }

    #endregion

    #region UnregisterProcessor Tests

    [Fact]
    public void UnregisterProcessor_WithNullProcessor_ShouldThrowArgumentNullException()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _consumer.UnregisterProcessor(null!));
    }

    [Fact]
    public void UnregisterProcessor_WithRegisteredProcessor_ShouldDecreaseProcessorCount()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        var processor = Substitute.For<ITestCompletionQueueProcessor>();
        _consumer.RegisterProcessor(processor);

        // Act
        _consumer.UnregisterProcessor(processor);

        // Assert
        _consumer.ProcessorCount.ShouldBe(0);
    }

    [Fact]
    public void UnregisterProcessor_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        var processor = Substitute.For<ITestCompletionQueueProcessor>();
        _consumer.RegisterProcessor(processor);
        _consumer.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => _consumer.UnregisterProcessor(processor));
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_ShouldStartSuccessfully()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);

        // Act
        await _consumer.StartAsync(CancellationToken.None);

        // Assert
        _consumer.IsRunning.ShouldBeTrue();
        _logger.Received().Log(LogLevel.Information, 
            Arg.Is<string>(s => s.Contains("Test completion queue consumer service started successfully")));
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        await _consumer.StartAsync(CancellationToken.None);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => 
            _consumer.StartAsync(CancellationToken.None));
        exception.Message.ShouldContain("already running");
    }

    [Fact]
    public async Task StartAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        _consumer.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() => 
            _consumer.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => 
            _consumer.StartAsync(cts.Token));
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenNotRunning_ShouldNotThrow()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);

        // Act & Assert - Should not throw
        await _consumer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_WhenRunning_ShouldStopSuccessfully()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestCompletionQueueMessage?>(null)); // Queue completed
        await _consumer.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Give processing loop time to start

        // Act
        await _consumer.StopAsync(CancellationToken.None);

        // Assert
        _consumer.IsRunning.ShouldBeFalse();
        _logger.Received().Log(LogLevel.Information, 
            Arg.Is<string>(s => s.Contains("Test completion queue consumer service stopped successfully")));
    }

    [Fact]
    public async Task StopAsync_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        _consumer.Dispose();

        // Act & Assert - Should not throw
        await _consumer.StopAsync(CancellationToken.None);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldDisposeCleanly()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);

        // Act
        _consumer.Dispose();

        // Assert
        _consumer.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeCleanly()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);

        // Act
        await _consumer.DisposeAsync();

        // Assert
        _consumer.IsRunning.ShouldBeFalse();
    }

    #endregion

    #region Exception Tests

    [Fact]
    public async Task ProcessMessagesAsync_WithNullMessage_ShouldHandleGracefully()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        var processor = Substitute.For<ITestCompletionQueueProcessor>();
        _consumer.RegisterProcessor(processor);

        // Setup queue to return null (queue completed)
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestCompletionQueueMessage?>(null));

        // Act
        await _consumer.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Give processing loop time to process
        await _consumer.StopAsync(CancellationToken.None);

        // Assert - Should not throw, processor should not be called
        await processor.DidNotReceive().ProcessTestCompletion(Arg.Any<TestCompletionQueueMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessagesAsync_WhenProcessorThrowsException_ShouldLogAndContinue()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        var processor = Substitute.For<ITestCompletionQueueProcessor>();
        var message = TestCompletionQueueConsumerTests.CreateTestMessage();

        processor.ProcessTestCompletion(Arg.Any<TestCompletionQueueMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Processor error")));

        _consumer.RegisterProcessor(processor);

        // Setup queue to return message then null (which will cause the processing loop to exit naturally)
        var callCount = 0;
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(_ => callCount++ == 0 ? Task.FromResult<TestCompletionQueueMessage?>(message) : Task.FromResult<TestCompletionQueueMessage?>(null));

        // Act
        var processingTask = _consumer.StartAsync(CancellationToken.None);
        // Wait long enough for all 3 retry attempts: initial + 100ms delay + attempt2 + 200ms delay + attempt3
        // Total: ~800ms to be safe, then stop
        await Task.Delay(1000);
        await _consumer.StopAsync(CancellationToken.None);
        await processingTask;

        // Assert - Should retry 3 times and log warnings for first 2 attempts, then error on final attempt
        // The processor will be called 3 times due to retry logic
        await processor.Received(3).ProcessTestCompletion(message, Arg.Any<CancellationToken>());

        // Should log warnings for first 2 attempts
        _logger.Received(2).Log(LogLevel.Warning, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());

        // Should log error on final attempt with exception
        _logger.Received(1).Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());

        // Should log permanent failure message (without exception)
        _logger.Received(1).Log(LogLevel.Error, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessMessagesAsync_WhenProcessorThrowsOperationCanceledException_ShouldLogWarning()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        var processor = Substitute.For<ITestCompletionQueueProcessor>();
        var message = TestCompletionQueueConsumerTests.CreateTestMessage();

        processor.ProcessTestCompletion(Arg.Any<TestCompletionQueueMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new OperationCanceledException()));

        _consumer.RegisterProcessor(processor);

        // Setup queue to return message then null
        var callCount = 0;
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(_ => callCount++ == 0 ? Task.FromResult<TestCompletionQueueMessage?>(message) : Task.FromResult<TestCompletionQueueMessage?>(null));

        // Act
        await _consumer.StartAsync(CancellationToken.None);
        await Task.Delay(200); // Give processing loop time to process
        await _consumer.StopAsync(CancellationToken.None);

        // Assert - Should log warning for cancellation
        _logger.Received().Log(LogLevel.Warning, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task ProcessMessagesAsync_WithMultipleProcessors_ShouldCallAllProcessors()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        var processor1 = Substitute.For<ITestCompletionQueueProcessor>();
        var processor2 = Substitute.For<ITestCompletionQueueProcessor>();
        var message = TestCompletionQueueConsumerTests.CreateTestMessage();

        _consumer.RegisterProcessor(processor1);
        _consumer.RegisterProcessor(processor2);

        // Setup queue to return message then null
        var callCount = 0;
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(_ => callCount++ == 0 ? Task.FromResult<TestCompletionQueueMessage?>(message) : Task.FromResult<TestCompletionQueueMessage?>(null));

        // Act
        await _consumer.StartAsync(CancellationToken.None);
        await Task.Delay(200); // Give processing loop time to process
        await _consumer.StopAsync(CancellationToken.None);

        // Assert - Both processors should be called
        await processor1.Received(1).ProcessTestCompletion(message, Arg.Any<CancellationToken>());
        await processor2.Received(1).ProcessTestCompletion(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessMessagesAsync_WhenOneProcessorFails_ShouldContinueWithOtherProcessors()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        var processor1 = Substitute.For<ITestCompletionQueueProcessor>();
        var processor2 = Substitute.For<ITestCompletionQueueProcessor>();
        var message = TestCompletionQueueConsumerTests.CreateTestMessage();

        // First processor throws exception
        processor1.ProcessTestCompletion(Arg.Any<TestCompletionQueueMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Processor 1 error")));

        _consumer.RegisterProcessor(processor1);
        _consumer.RegisterProcessor(processor2);

        // Setup queue to return message then null (which will cause the processing loop to exit naturally)
        var callCount = 0;
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(_ => callCount++ == 0 ? Task.FromResult<TestCompletionQueueMessage?>(message) : Task.FromResult<TestCompletionQueueMessage?>(null));

        // Act
        var processingTask = _consumer.StartAsync(CancellationToken.None);
        // Wait long enough for all 3 retry attempts: initial + 100ms delay + attempt2 + 200ms delay + attempt3
        // Total: ~1000ms to be safe, then stop
        await Task.Delay(1000);
        await _consumer.StopAsync(CancellationToken.None);
        await processingTask;

        // Assert - Second processor should still be called despite first processor failing
        // First processor will be retried 3 times due to retry logic
        await processor1.Received(3).ProcessTestCompletion(message, Arg.Any<CancellationToken>());
        // Second processor should be called once successfully
        await processor2.Received(1).ProcessTestCompletion(message, Arg.Any<CancellationToken>());
        // Should log errors for processor1's failures
        _logger.Received().Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public async Task Dispose_WhenRunning_ShouldStopProcessing()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestCompletionQueueMessage?>(null));
        await _consumer.StartAsync(CancellationToken.None);

        // Act
        _consumer.Dispose();

        // Assert
        _consumer.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenRunning_ShouldStopProcessing()
    {
        // Arrange
        _consumer = new TestCompletionQueueConsumer(_queue, _logger);
        _queue.DequeueAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TestCompletionQueueMessage?>(null));
        await _consumer.StartAsync(CancellationToken.None);

        // Act
        await _consumer.DisposeAsync();

        // Assert
        _consumer.IsRunning.ShouldBeFalse();
    }

    #endregion

    #region Helper Methods

    private static TestCompletionQueueMessage CreateTestMessage(string testCaseId = "TestClass.TestMethod()")
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

    #endregion
}

