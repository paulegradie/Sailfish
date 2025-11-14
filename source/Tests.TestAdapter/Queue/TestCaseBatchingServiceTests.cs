using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
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

        // Act
        await _batchingService.StartAsync(CancellationToken.None);

        // Assert
        _logger.Received(1).Log(LogLevel.Information,
            "TestCaseBatchingService started with strategy '{0}'",
            Arg.Any<object[]>());
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

        // Act
        await _batchingService.StopAsync(CancellationToken.None);

        // Assert
        _logger.Received(1).Log(LogLevel.Information,
            "TestCaseBatchingService stopped. Total batches managed: {0}",
            Arg.Any<object[]>());
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

        // Act
        _batchingService.Dispose();

        // Assert
        _logger.Received(1).Log(LogLevel.Debug,
            "TestCaseBatchingService disposed successfully");
    }

    // TestCaseBatchingService doesn't implement IAsyncDisposable

    #endregion

    #region SetBatchingStrategyAsync Tests

    [Fact]
    public async Task SetBatchingStrategyAsync_WithValidStrategy_ShouldUpdateStrategy()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);

        // Act
        await _batchingService.SetBatchingStrategyAsync(BatchingStrategy.ByComparisonAttribute, CancellationToken.None);

        // Assert
        var currentStrategy = await _batchingService.GetBatchingStrategyAsync(CancellationToken.None);
        currentStrategy.ShouldBe(BatchingStrategy.ByComparisonAttribute);
    }

    [Fact]
    public async Task SetBatchingStrategyAsync_WithActiveBatches_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);
        var message = CreateTestMessage();
        await _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _batchingService.SetBatchingStrategyAsync(BatchingStrategy.ByComparisonAttribute, CancellationToken.None));
    }

    [Fact]
    public async Task SetBatchingStrategyAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _batchingService.SetBatchingStrategyAsync(BatchingStrategy.ByComparisonAttribute, cts.Token));
    }

    #endregion

    #region GetBatchingStrategyAsync Tests

    [Fact]
    public async Task GetBatchingStrategyAsync_ShouldReturnDefaultStrategy()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);

        // Act
        var strategy = await _batchingService.GetBatchingStrategyAsync(CancellationToken.None);

        // Assert
        strategy.ShouldBe(BatchingStrategy.ByTestClass);
    }

    [Fact]
    public async Task GetBatchingStrategyAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _batchingService.GetBatchingStrategyAsync(cts.Token));
    }

    #endregion

    #region GetAllBatchesAsync Tests

    [Fact]
    public async Task GetAllBatchesAsync_WithNoBatches_ShouldReturnEmptyCollection()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);

        // Act
        var batches = await _batchingService.GetAllBatchesAsync(CancellationToken.None);

        // Assert
        batches.ShouldNotBeNull();
        batches.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllBatchesAsync_WithBatches_ShouldReturnAllBatches()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);
        var message1 = CreateTestMessage("TestClass1.Method1()");
        var message2 = CreateTestMessage("TestClass2.Method1()");
        await _batchingService.AddTestCaseToBatchAsync(message1, CancellationToken.None);
        await _batchingService.AddTestCaseToBatchAsync(message2, CancellationToken.None);

        // Act
        var batches = await _batchingService.GetAllBatchesAsync(CancellationToken.None);

        // Assert
        batches.ShouldNotBeNull();
        batches.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllBatchesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _batchingService.GetAllBatchesAsync(cts.Token));
    }

    #endregion

    #region Batching Strategy Tests

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithByComparisonAttributeStrategy_ShouldGroupByComparisonGroup()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.SetBatchingStrategyAsync(BatchingStrategy.ByComparisonAttribute, CancellationToken.None);
        await _batchingService.StartAsync(CancellationToken.None);

        var message1 = CreateTestMessageWithComparisonGroup("TestClass.Method1()", "GroupA");
        var message2 = CreateTestMessageWithComparisonGroup("TestClass.Method2()", "GroupA");
        var message3 = CreateTestMessageWithComparisonGroup("TestClass.Method3()", "GroupB");

        // Act
        var batchId1 = await _batchingService.AddTestCaseToBatchAsync(message1, CancellationToken.None);
        var batchId2 = await _batchingService.AddTestCaseToBatchAsync(message2, CancellationToken.None);
        var batchId3 = await _batchingService.AddTestCaseToBatchAsync(message3, CancellationToken.None);

        // Assert
        batchId1.ShouldBe(batchId2); // Same comparison group
        batchId1.ShouldNotBe(batchId3); // Different comparison group
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithByCustomCriteriaStrategy_ShouldGroupByCustomCriteria()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.SetBatchingStrategyAsync(BatchingStrategy.ByCustomCriteria, CancellationToken.None);
        await _batchingService.StartAsync(CancellationToken.None);

        var message1 = CreateTestMessageWithCustomCriteria("TestClass.Method1()", "CustomGroup1");
        var message2 = CreateTestMessageWithCustomCriteria("TestClass.Method2()", "CustomGroup1");
        var message3 = CreateTestMessageWithCustomCriteria("TestClass.Method3()", "CustomGroup2");

        // Act
        var batchId1 = await _batchingService.AddTestCaseToBatchAsync(message1, CancellationToken.None);
        var batchId2 = await _batchingService.AddTestCaseToBatchAsync(message2, CancellationToken.None);
        var batchId3 = await _batchingService.AddTestCaseToBatchAsync(message3, CancellationToken.None);

        // Assert
        batchId1.ShouldBe(batchId2); // Same custom criteria
        batchId1.ShouldNotBe(batchId3); // Different custom criteria
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithByExecutionContextStrategy_ShouldGroupByExecutionContext()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.SetBatchingStrategyAsync(BatchingStrategy.ByExecutionContext, CancellationToken.None);
        await _batchingService.StartAsync(CancellationToken.None);

        var message1 = CreateTestMessageWithExecutionContext("TestClass.Method1()", "Context1");
        var message2 = CreateTestMessageWithExecutionContext("TestClass.Method2()", "Context1");
        var message3 = CreateTestMessageWithExecutionContext("TestClass.Method3()", "Context2");

        // Act
        var batchId1 = await _batchingService.AddTestCaseToBatchAsync(message1, CancellationToken.None);
        var batchId2 = await _batchingService.AddTestCaseToBatchAsync(message2, CancellationToken.None);
        var batchId3 = await _batchingService.AddTestCaseToBatchAsync(message3, CancellationToken.None);

        // Assert
        batchId1.ShouldBe(batchId2); // Same execution context
        batchId1.ShouldNotBe(batchId3); // Different execution context
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithByPerformanceProfileStrategy_ShouldGroupByPerformanceProfile()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.SetBatchingStrategyAsync(BatchingStrategy.ByPerformanceProfile, CancellationToken.None);
        await _batchingService.StartAsync(CancellationToken.None);

        var message1 = CreateTestMessageWithPerformanceMetrics("TestClass.Method1()", 5.0); // Fast
        var message2 = CreateTestMessageWithPerformanceMetrics("TestClass.Method2()", 8.0); // Fast
        var message3 = CreateTestMessageWithPerformanceMetrics("TestClass.Method3()", 50.0); // Medium

        // Act
        var batchId1 = await _batchingService.AddTestCaseToBatchAsync(message1, CancellationToken.None);
        var batchId2 = await _batchingService.AddTestCaseToBatchAsync(message2, CancellationToken.None);
        var batchId3 = await _batchingService.AddTestCaseToBatchAsync(message3, CancellationToken.None);

        // Assert
        batchId1.ShouldBe(batchId2); // Same performance profile (Fast)
        batchId1.ShouldNotBe(batchId3); // Different performance profile (Fast vs Medium)
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithNoneStrategy_ShouldCreateIndividualBatches()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.SetBatchingStrategyAsync(BatchingStrategy.None, CancellationToken.None);
        await _batchingService.StartAsync(CancellationToken.None);

        var message1 = CreateTestMessage("TestClass.Method1()");
        var message2 = CreateTestMessage("TestClass.Method2()");

        // Act
        var batchId1 = await _batchingService.AddTestCaseToBatchAsync(message1, CancellationToken.None);
        var batchId2 = await _batchingService.AddTestCaseToBatchAsync(message2, CancellationToken.None);

        // Assert
        batchId1.ShouldNotBe(batchId2); // Each test gets its own batch
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task AddTestCaseToBatchAsync_WhenNotStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        var message = CreateTestMessage();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None));
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WhenCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);
        await _batchingService.StopAsync(CancellationToken.None);
        var message = CreateTestMessage();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _batchingService.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Operations_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        _batchingService.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() => _batchingService.StartAsync(CancellationToken.None));
    }

    #endregion

    #region IsBatchCompleteAsync Tests

    [Fact]
    public async Task IsBatchCompleteAsync_WithNonExistentBatch_ShouldThrowArgumentException()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _batchingService.IsBatchCompleteAsync("NonExistentBatch", CancellationToken.None));
    }

    [Fact]
    public async Task IsBatchCompleteAsync_WithPendingBatch_ShouldReturnFalse()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);
        var message = CreateTestMessage();
        var batchId = await _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None);

        // Act
        var isComplete = await _batchingService.IsBatchCompleteAsync(batchId, CancellationToken.None);

        // Assert
        isComplete.ShouldBeFalse();
    }

    [Fact]
    public async Task IsBatchCompleteAsync_WithNoneStrategy_ShouldReturnTrueAfterAddingTestCase()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.SetBatchingStrategyAsync(BatchingStrategy.None, CancellationToken.None);
        await _batchingService.StartAsync(CancellationToken.None);
        var message = CreateTestMessage();
        var batchId = await _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None);

        // Act
        var isComplete = await _batchingService.IsBatchCompleteAsync(batchId, CancellationToken.None);

        // Assert
        isComplete.ShouldBeTrue(); // None strategy means each test case is its own complete batch
    }

    [Fact]
    public async Task IsBatchCompleteAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _batchingService.IsBatchCompleteAsync("SomeBatch", cts.Token));
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithEmptyTestCaseId_ShouldHandleGracefully()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);
        var message = CreateTestMessage("");

        // Act
        var batchId = await _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None);

        // Assert
        batchId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithNullMetadata_ShouldHandleGracefully()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);
        var message = CreateTestMessage();
        message.Metadata = new Dictionary<string, object>();

        // Act & Assert - Should not throw
        var batchId = await _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None);
        batchId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AddTestCaseToBatchAsync_WithNullPerformanceMetrics_ShouldHandleGracefully()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);
        var message = CreateTestMessage();
        message.PerformanceMetrics = null!;

        // Act & Assert - Should not throw
        var batchId = await _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None);
        batchId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCompletedBatchesAsync_AfterStopAsync_ShouldReturnAllBatchesAsCompleted()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);
        var message1 = CreateTestMessage("TestClass1.Method1()");
        var message2 = CreateTestMessage("TestClass2.Method1()");
        await _batchingService.AddTestCaseToBatchAsync(message1, CancellationToken.None);
        await _batchingService.AddTestCaseToBatchAsync(message2, CancellationToken.None);

        // Act
        await _batchingService.StopAsync(CancellationToken.None);
        var completedBatches = await _batchingService.GetCompletedBatchesAsync(CancellationToken.None);

        // Assert
        completedBatches.ShouldNotBeEmpty();
        completedBatches.Count().ShouldBe(2);
        completedBatches.All(b => b.Status == BatchStatus.Complete).ShouldBeTrue();
    }

    [Fact]
    public async Task Dispose_WithRunningService_ShouldStopServiceGracefully()
    {
        // Arrange
        _batchingService = new TestCaseBatchingService(_logger);
        await _batchingService.StartAsync(CancellationToken.None);
        var message = CreateTestMessage();
        await _batchingService.AddTestCaseToBatchAsync(message, CancellationToken.None);

        // Act
        _batchingService.Dispose();

        // Assert
        // Should log both stop and dispose messages
        _logger.Received(1).Log(LogLevel.Information,
            "TestCaseBatchingService stopped. Total batches managed: {0}",
            Arg.Any<object[]>());
        _logger.Received(1).Log(LogLevel.Debug,
            "TestCaseBatchingService disposed successfully");
    }

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

    private TestCompletionQueueMessage CreateTestMessageWithComparisonGroup(string testCaseId, string comparisonGroup)
    {
        var message = CreateTestMessage(testCaseId);
        message.Metadata["ComparisonGroup"] = comparisonGroup;
        message.PerformanceMetrics = new PerformanceMetrics { GroupingId = comparisonGroup };
        return message;
    }

    private TestCompletionQueueMessage CreateTestMessageWithCustomCriteria(string testCaseId, string customCriteria)
    {
        var message = CreateTestMessage(testCaseId);
        message.Metadata["BatchingCriteria"] = customCriteria;
        return message;
    }

    private TestCompletionQueueMessage CreateTestMessageWithExecutionContext(string testCaseId, string executionContext)
    {
        var message = CreateTestMessage(testCaseId);
        message.Metadata["ExecutionContext"] = executionContext;
        return message;
    }

    private TestCompletionQueueMessage CreateTestMessageWithPerformanceMetrics(string testCaseId, double meanMs)
    {
        var message = CreateTestMessage(testCaseId);
        message.PerformanceMetrics = new PerformanceMetrics { MeanMs = meanMs };
        return message;
    }

    #endregion
}
