using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for BatchTimeoutHandler.
/// Tests timeout monitoring, batch processing, and error handling scenarios.
/// </summary>
public class BatchTimeoutHandlerTests : IDisposable
{
    private readonly ITestCaseBatchingService _batchingService;
    private readonly IMediator _mediator;
    private readonly QueueConfiguration _configuration;
    private readonly ILogger _logger;
    private BatchTimeoutHandler? _timeoutHandler;

    public BatchTimeoutHandlerTests()
    {
        _batchingService = Substitute.For<ITestCaseBatchingService>();
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger>();
        
        _configuration = new QueueConfiguration
        {
            BatchCompletionTimeoutMs = 5000,
            ProcessingTimeoutMs = 30000,
            MaxRetryAttempts = 3
        };
    }

    public void Dispose()
    {
        _timeoutHandler?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullBatchingService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new BatchTimeoutHandler(null!, _mediator, _configuration, _logger));
    }

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new BatchTimeoutHandler(_batchingService, null!, _configuration, _logger));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new BatchTimeoutHandler(_batchingService, _mediator, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new BatchTimeoutHandler(_batchingService, _mediator, _configuration, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);

        // Assert
        _timeoutHandler.ShouldNotBeNull();
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_ShouldStartTimeoutMonitoring()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);

        // Act & Assert - Should not throw
        await _timeoutHandler.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        await _timeoutHandler.StartAsync(CancellationToken.None);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _timeoutHandler.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _timeoutHandler.StartAsync(cts.Token));
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);

        // Act & Assert - Should not throw
        await _timeoutHandler.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_WhenStarted_ShouldStopTimeoutMonitoring()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        await _timeoutHandler.StartAsync(CancellationToken.None);

        // Act & Assert - Should not throw
        await _timeoutHandler.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        await _timeoutHandler.StartAsync(CancellationToken.None);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _timeoutHandler.StopAsync(cts.Token));
    }

    #endregion

    #region ProcessTimedOutBatchesAsync Tests

    [Fact]
    public async Task ProcessTimedOutBatchesAsync_WithNoPendingBatches_ShouldReturnZero()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        _batchingService.GetPendingBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TestCaseBatch>());

        // Act
        var result = await _timeoutHandler.ProcessTimedOutBatchesAsync(CancellationToken.None);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessTimedOutBatchesAsync_WithNonTimedOutBatches_ShouldReturnZero()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        var recentBatch = CreateTestBatch("batch1", DateTime.UtcNow.AddSeconds(-1)); // Recent batch
        _batchingService.GetPendingBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TestCaseBatch> { recentBatch });

        // Act
        var result = await _timeoutHandler.ProcessTimedOutBatchesAsync(CancellationToken.None);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessTimedOutBatchesAsync_WithTimedOutBatches_ShouldProcessAndReturnCount()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        var timedOutBatch = CreateTestBatch("batch1", DateTime.UtcNow.AddMinutes(-10)); // Old batch

        // Ensure the batch has no completion timeout set so it uses the global configuration
        timedOutBatch.CompletionTimeout = null;

        // Debug: Verify the batch should be considered timed out
        var elapsed = DateTime.UtcNow - timedOutBatch.CreatedAt;
        var timeout = TimeSpan.FromMilliseconds(_configuration.BatchCompletionTimeoutMs);
        elapsed.ShouldBeGreaterThan(timeout, $"Batch should be timed out. Elapsed: {elapsed}, Timeout: {timeout}");

        // Debug: Verify the test message has the required metadata
        var testMessage = timedOutBatch.TestCases.First();
        testMessage.Metadata.ShouldContainKey("TestCase");
        testMessage.Metadata["TestCase"].ShouldBeOfType<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase>();

        // Debug: Verify batch structure
        timedOutBatch.Status.ShouldBe(BatchStatus.Pending);
        timedOutBatch.TestCases.Count.ShouldBe(1);

        _batchingService.GetPendingBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<TestCaseBatch> { timedOutBatch });

        // Act & Assert - Catch any exceptions to debug
        Exception? caughtException = null;
        int result = 0;
        try
        {
            result = await _timeoutHandler.ProcessTimedOutBatchesAsync(CancellationToken.None);
            result.ShouldBe(1);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // If an exception was caught, the test should fail with details
        if (caughtException != null)
        {
            throw new Exception($"ProcessTimedOutBatchesAsync threw an exception: {caughtException.Message}", caughtException);
        }

        // Debug: Check what calls were actually made to the mediator
        var receivedCalls = _mediator.ReceivedCalls().ToList();

        // Debug: Check what was actually returned by GetPendingBatchesAsync
        var actualBatches = await _batchingService.GetPendingBatchesAsync(CancellationToken.None);
        var actualBatch = actualBatches.FirstOrDefault();

        // The test should pass - the method is working correctly
        result.ShouldBe(1);
        await _mediator.Received(1).Publish(Arg.Any<FrameworkTestCaseEndNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessTimedOutBatchesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => 
            _timeoutHandler.ProcessTimedOutBatchesAsync(cts.Token));
    }

    [Fact]
    public async Task ProcessTimedOutBatchesAsync_WhenBatchingServiceThrows_ShouldHandleGracefully()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        _batchingService.GetPendingBatchesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<TestCaseBatch>>(new InvalidOperationException("Service error")));

        // Act
        var result = await _timeoutHandler.ProcessTimedOutBatchesAsync(CancellationToken.None);

        // Assert
        result.ShouldBe(0);
        _logger.Received().Log(LogLevel.Error, Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldDisposeResourcesProperly()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);

        // Act & Assert - Should not throw
        _timeoutHandler.Dispose();
    }

    // BatchTimeoutHandler doesn't implement IAsyncDisposable

    [Fact]
    public async Task Dispose_WhenStarted_ShouldStopFirst()
    {
        // Arrange
        _timeoutHandler = new BatchTimeoutHandler(_batchingService, _mediator, _configuration, _logger);
        await _timeoutHandler.StartAsync(CancellationToken.None);

        // Act & Assert - Should not throw
        _timeoutHandler.Dispose();
    }

    #endregion

    #region Helper Methods

    private TestCaseBatch CreateTestBatch(string batchId, DateTime createdAt)
    {
        var testMessage = CreateTestMessage();
        return new TestCaseBatch
        {
            BatchId = batchId,
            CreatedAt = createdAt,
            Status = BatchStatus.Pending,
            TestCases = new List<TestCompletionQueueMessage> { testMessage }
        };
    }

    private TestCompletionQueueMessage CreateTestMessage()
    {
        var testCase = new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase(
            "TestClass.TestMethod()",
            new Uri("executor://sailfish"),
            "Sailfish");

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
                ["TestCase"] = testCase,
                ["FormattedMessage"] = "Test completed successfully",
                ["StartTime"] = DateTimeOffset.UtcNow.AddSeconds(-1),
                ["EndTime"] = DateTimeOffset.UtcNow
            },
            PerformanceMetrics = new PerformanceMetrics()
        };
    }

    #endregion
}
