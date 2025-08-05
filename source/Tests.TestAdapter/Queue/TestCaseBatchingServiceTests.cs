using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for TestCaseBatchingService.
/// Tests batching strategies, batch management, and completion detection.
/// </summary>
public class TestCaseBatchingServiceTests : IDisposable
{
    private readonly QueueConfiguration _configuration;
    private readonly ILogger _logger;
    private TestCaseBatchingService? _batchingService;

    public TestCaseBatchingServiceTests()
    {
        _configuration = new QueueConfiguration
        {
            MaxBatchSize = 10,
            BatchCompletionTimeoutMs = 30000,
            EnableBatchProcessing = true
        };
        _logger = Substitute.For<ILogger>();
    }

    public void Dispose()
    {
        _batchingService?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestCaseBatchingService(null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        _batchingService = new TestCaseBatchingService(_logger);

        // Assert
        _batchingService.ShouldNotBeNull();
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_ShouldStartService()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);

        // Act & Assert - Should not throw
        await _batchingService.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => _batchingService.StartAsync(cts.Token));
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_ShouldStopService()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);

        // Act & Assert - Should not throw
        await _batchingService.StopAsync(CancellationToken.None);
    }

    #endregion

    #region AddTestCaseToBatchAsync Tests

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _batchingService.AddTestCaseToBatchAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithValidMessage_ShouldReturnBatchId()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        var message = CreateTestMessage();
        await _batchingService.StartAsync(CancellationToken.None);

        // Act
        var batchId = await _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None);

        // Assert
        batchId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithSameTestClass_ShouldUseSameBatch()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        var message1 = CreateTestMessage("TestClass.Method1()");
        var message2 = CreateTestMessage("TestClass.Method2()");
        await _batchingService.StartAsync(CancellationToken.None);

        // Act
        var batchId1 = await _batchingService.AddTestCaseToBatchAsync(message1, CancellationToken.None);
        var batchId2 = await _batchingService.AddTestCaseToBatchAsync(message2, CancellationToken.None);

        // Assert
        batchId1.ShouldBe(batchId2);
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithDifferentTestClass_ShouldUseDifferentBatch()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        var message1 = CreateTestMessage("TestClass1.Method1()");
        var message2 = CreateTestMessage("TestClass2.Method1()");
        await _batchingService.StartAsync(CancellationToken.None);

        // Act
        var batchId1 = await _batchingService.AddTestCaseToBatchAsync(message1, CancellationToken.None);
        var batchId2 = await _batchingService.AddTestCaseToBatchAsync(message2, CancellationToken.None);

        // Assert
        batchId1.ShouldNotBe(batchId2);
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(); // Start the service first
        var message = CreateTestMessage();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _batchingService.AddTestCaseToBatchAsync(message, cts.Token));
    }

    #endregion

    #region GetPendingBatchesAsync Tests

    [Fact]
    public async Task GetPendingBatchesAsync_WithNoBatches_ShouldReturnEmptyList()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);

        // Act
        var batches = await _batchingService.GetPendingBatchesAsync(CancellationToken.None);

        // Assert
        batches.ShouldNotBeNull();
        batches.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingBatchesAsync_WithPendingBatches_ShouldReturnBatches()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        var message = CreateTestMessage();
        await _batchingService.StartAsync(CancellationToken.None);
        await _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None);

        // Act
        var batches = await _batchingService.GetPendingBatchesAsync(CancellationToken.None);

        // Assert
        batches.ShouldNotBeEmpty();
        batches.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GetPendingBatchesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _batchingService.GetPendingBatchesAsync(cts.Token));
    }

    #endregion

    #region GetCompletedBatchesAsync Tests

    [Fact]
    public async Task GetCompletedBatchesAsync_WithNoCompletedBatches_ShouldReturnEmptyList()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);

        // Act
        var batches = await _batchingService.GetCompletedBatchesAsync(CancellationToken.None);

        // Assert
        batches.ShouldNotBeNull();
        batches.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCompletedBatchesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => 
            _batchingService.GetCompletedBatchesAsync(cts.Token));
    }

    #endregion

    // MarkBatchAsCompletedAsync method doesn't exist on TestCaseBatchingService

    #region Disposal Tests

    [Fact]
    public void Dispose_ShouldDisposeResourcesProperly()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);

        // Act & Assert - Should not throw
        _batchingService.Dispose();
    }

    // TestCaseBatchingService doesn't implement IAsyncDisposable

    #endregion

    #region Helper Methods

    private TestCompletionQueueMessage CreateTestMessage(string? testCaseId = null)
    {
        return new TestCompletionQueueMessage
        {
            TestCaseId = testCaseId ?? "TestClass.TestMethod()",
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
