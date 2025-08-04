using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Extensions;
using Sailfish.TestAdapter.Queue.Implementation;
using Sailfish.TestAdapter.Queue.Processors;
using Shouldly;
using Xunit;
using ILogger = Sailfish.Logging.ILogger;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive unit tests for the queue infrastructure components including
/// message contracts, queue operations, batching services, and processors.
/// These tests validate the core functionality of the intercepting queue architecture
/// that enables batch processing and cross-test-case analysis.
/// </summary>
public class QueueInfrastructureTests
{
    #region Test Data Builders

    /// <summary>
    /// Creates a test completion queue message with default values for testing.
    /// </summary>
    private static TestCompletionQueueMessage CreateTestMessage(string testCaseId = "TestCase1")
    {
        return new TestCompletionQueueMessage
        {
            TestCaseId = testCaseId,
            TestResult = new TestExecutionResult
            {
                IsSuccess = true,
                ExceptionMessage = null,
                ExceptionDetails = null,
                ExceptionType = null
            },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["TestClass"] = "TestClass1",
                ["TestMethod"] = "TestMethod1"
            },
            PerformanceMetrics = new PerformanceMetrics
            {
                MeanMs = 100.5,
                MedianMs = 95.2,
                StandardDeviation = 15.3,
                Variance = 234.09,
                RawExecutionResults = new[] { 90.1, 95.2, 100.5, 105.8, 110.3 },
                DataWithOutliersRemoved = new[] { 95.2, 100.5, 105.8 },
                LowerOutliers = new[] { 90.1 },
                UpperOutliers = new[] { 110.3 },
                TotalNumOutliers = 2,
                SampleSize = 5,
                NumWarmupIterations = 3,
                GroupingId = "Group1"
            }
        };
    }

    /// <summary>
    /// Creates a failed test completion queue message for testing error scenarios.
    /// </summary>
    private static TestCompletionQueueMessage CreateFailedTestMessage(string testCaseId = "FailedTest1")
    {
        return new TestCompletionQueueMessage
        {
            TestCaseId = testCaseId,
            TestResult = new TestExecutionResult
            {
                IsSuccess = false,
                ExceptionMessage = "Test failed with assertion error",
                ExceptionDetails = "System.Exception: Test failed with assertion error\n   at TestMethod()",
                ExceptionType = "System.Exception"
            },
            CompletedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["TestClass"] = "FailedTestClass",
                ["TestMethod"] = "FailedTestMethod"
            },
            PerformanceMetrics = new PerformanceMetrics()
        };
    }

    #endregion

    #region TestCompletionQueueMessage Tests

    /// <summary>
    /// Tests that TestCompletionQueueMessage can be properly serialized and deserialized.
    /// </summary>
    [Fact]
    public void TestCompletionQueueMessage_JsonSerialization_ShouldWorkCorrectly()
    {
        // Arrange
        var originalMessage = CreateTestMessage();

        // Act
        var json = JsonSerializer.Serialize(originalMessage);
        var deserializedMessage = JsonSerializer.Deserialize<TestCompletionQueueMessage>(json);

        // Assert
        deserializedMessage.ShouldNotBeNull();
        deserializedMessage.TestCaseId.ShouldBe(originalMessage.TestCaseId);
        deserializedMessage.TestResult.IsSuccess.ShouldBe(originalMessage.TestResult.IsSuccess);
        deserializedMessage.CompletedAt.ShouldBe(originalMessage.CompletedAt);
        deserializedMessage.Metadata.Count.ShouldBe(originalMessage.Metadata.Count);
        deserializedMessage.PerformanceMetrics.MeanMs.ShouldBe(originalMessage.PerformanceMetrics.MeanMs);
    }

    /// <summary>
    /// Tests that TestCompletionQueueMessage properties are initialized with default values.
    /// </summary>
    [Fact]
    public void TestCompletionQueueMessage_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var message = new TestCompletionQueueMessage();

        // Assert
        message.TestCaseId.ShouldBe(string.Empty);
        message.TestResult.ShouldNotBeNull();
        message.Metadata.ShouldNotBeNull();
        message.PerformanceMetrics.ShouldNotBeNull();
        message.CompletedAt.ShouldBe(default(DateTime));
    }

    /// <summary>
    /// Tests that TestExecutionResult properly represents successful test execution.
    /// </summary>
    [Fact]
    public void TestExecutionResult_SuccessfulTest_ShouldHaveCorrectProperties()
    {
        // Arrange
        var result = new TestExecutionResult
        {
            IsSuccess = true,
            ExceptionMessage = null,
            ExceptionDetails = null,
            ExceptionType = null
        };

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.ExceptionMessage.ShouldBeNull();
        result.ExceptionDetails.ShouldBeNull();
        result.ExceptionType.ShouldBeNull();
    }

    /// <summary>
    /// Tests that TestExecutionResult properly represents failed test execution.
    /// </summary>
    [Fact]
    public void TestExecutionResult_FailedTest_ShouldHaveExceptionDetails()
    {
        // Arrange
        var result = new TestExecutionResult
        {
            IsSuccess = false,
            ExceptionMessage = "Test failed",
            ExceptionDetails = "Full stack trace",
            ExceptionType = "System.Exception"
        };

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ExceptionMessage.ShouldBe("Test failed");
        result.ExceptionDetails.ShouldBe("Full stack trace");
        result.ExceptionType.ShouldBe("System.Exception");
    }

    /// <summary>
    /// Tests that PerformanceMetrics arrays are initialized as empty arrays.
    /// </summary>
    [Fact]
    public void PerformanceMetrics_DefaultConstructor_ShouldInitializeArrays()
    {
        // Act
        var metrics = new PerformanceMetrics();

        // Assert
        metrics.RawExecutionResults.ShouldNotBeNull();
        metrics.RawExecutionResults.ShouldBeEmpty();
        metrics.DataWithOutliersRemoved.ShouldNotBeNull();
        metrics.DataWithOutliersRemoved.ShouldBeEmpty();
        metrics.LowerOutliers.ShouldNotBeNull();
        metrics.LowerOutliers.ShouldBeEmpty();
        metrics.UpperOutliers.ShouldNotBeNull();
        metrics.UpperOutliers.ShouldBeEmpty();
    }

    #endregion

    #region InMemoryTestCompletionQueue Tests

    /// <summary>
    /// Tests that InMemoryTestCompletionQueue can be started successfully.
    /// </summary>
    [Fact]
    public async Task InMemoryTestCompletionQueue_Start_ShouldSetRunningState()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);

        // Act
        await queue.StartAsync(CancellationToken.None);

        // Assert
        queue.IsRunning.ShouldBeTrue();
        queue.IsCompleted.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that InMemoryTestCompletionQueue throws when started twice.
    /// </summary>
    [Fact]
    public async Task InMemoryTestCompletionQueue_StartTwice_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => queue.StartAsync(CancellationToken.None));
    }

    /// <summary>
    /// Tests that InMemoryTestCompletionQueue can enqueue and dequeue messages.
    /// </summary>
    [Fact]
    public async Task InMemoryTestCompletionQueue_EnqueueDequeue_ShouldWorkCorrectly()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);
        var message = CreateTestMessage();

        // Act
        await queue.EnqueueAsync(message, CancellationToken.None);
        var dequeuedMessage = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        dequeuedMessage.ShouldNotBeNull();
        dequeuedMessage.TestCaseId.ShouldBe(message.TestCaseId);
        dequeuedMessage.TestResult.IsSuccess.ShouldBe(message.TestResult.IsSuccess);
    }

    /// <summary>
    /// Tests that InMemoryTestCompletionQueue throws when enqueuing to a stopped queue.
    /// </summary>
    [Fact]
    public async Task InMemoryTestCompletionQueue_EnqueueAfterStop_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);
        await queue.StopAsync(CancellationToken.None);
        var message = CreateTestMessage();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => queue.EnqueueAsync(message, CancellationToken.None));
    }

    /// <summary>
    /// Tests that InMemoryTestCompletionQueue throws when constructed with invalid capacity.
    /// </summary>
    [Fact]
    public void InMemoryTestCompletionQueue_InvalidCapacity_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new InMemoryTestCompletionQueue(0));
        Should.Throw<ArgumentOutOfRangeException>(() => new InMemoryTestCompletionQueue(-1));
    }

    /// <summary>
    /// Tests that InMemoryTestCompletionQueue accepts valid capacity values.
    /// </summary>
    [Fact]
    public void InMemoryTestCompletionQueue_ValidCapacity_ShouldCreateSuccessfully()
    {
        // Act & Assert
        using var queue1 = new InMemoryTestCompletionQueue(1);
        using var queue2 = new InMemoryTestCompletionQueue(1000);
        using var queue3 = new InMemoryTestCompletionQueue(int.MaxValue);

        queue1.ShouldNotBeNull();
        queue2.ShouldNotBeNull();
        queue3.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that InMemoryTestCompletionQueue constructor with QueueConfiguration works correctly.
    /// </summary>
    [Fact]
    public void InMemoryTestCompletionQueue_ConfigurationConstructor_ShouldCreateSuccessfully()
    {
        // Arrange
        var config = new QueueConfiguration
        {
            MaxQueueCapacity = 500,
            EnableBatchProcessing = true,
            MaxBatchSize = 25
        };

        // Act
        using var queue = new InMemoryTestCompletionQueue(config);

        // Assert
        queue.ShouldNotBeNull();
        queue.Configuration.ShouldNotBeNull();
        queue.Configuration.MaxQueueCapacity.ShouldBe(500);
        queue.Configuration.EnableBatchProcessing.ShouldBeTrue();
        queue.Configuration.MaxBatchSize.ShouldBe(25);
        queue.MaxCapacity.ShouldBe(500);
    }

    /// <summary>
    /// Tests that InMemoryTestCompletionQueue throws when configuration is null.
    /// </summary>
    [Fact]
    public void InMemoryTestCompletionQueue_NullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new InMemoryTestCompletionQueue((QueueConfiguration)null!));
    }

    /// <summary>
    /// Tests that InMemoryTestCompletionQueue throws when configuration has invalid capacity.
    /// </summary>
    [Fact]
    public void InMemoryTestCompletionQueue_InvalidConfigurationCapacity_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var config1 = new QueueConfiguration { MaxQueueCapacity = 0 };
        var config2 = new QueueConfiguration { MaxQueueCapacity = -1 };

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => new InMemoryTestCompletionQueue(config1));
        Should.Throw<ArgumentOutOfRangeException>(() => new InMemoryTestCompletionQueue(config2));
    }

    /// <summary>
    /// Tests that legacy constructor sets Configuration property to null.
    /// </summary>
    [Fact]
    public void InMemoryTestCompletionQueue_LegacyConstructor_ShouldHaveNullConfiguration()
    {
        // Act
        using var queue = new InMemoryTestCompletionQueue(1000);

        // Assert
        queue.Configuration.ShouldBeNull();
        queue.MaxCapacity.ShouldBe(1000);
    }

    #endregion

    #region TestCompletionQueuePublisher Tests

    /// <summary>
    /// Tests that TestCompletionQueuePublisher publishes messages to the queue.
    /// </summary>
    [Fact]
    public async Task TestCompletionQueuePublisher_PublishTestCompletion_ShouldEnqueueMessage()
    {
        // Arrange
        var mockQueue = Substitute.For<ITestCompletionQueue>();
        var mockLogger = Substitute.For<ILogger>();
        var publisher = new TestCompletionQueuePublisher(mockQueue, mockLogger);
        var message = CreateTestMessage();

        // Act
        await publisher.PublishTestCompletion(message, CancellationToken.None);

        // Assert
        await mockQueue.Received(1).EnqueueAsync(message, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that TestCompletionQueuePublisher throws when message is null.
    /// </summary>
    [Fact]
    public async Task TestCompletionQueuePublisher_PublishNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockQueue = Substitute.For<ITestCompletionQueue>();
        var mockLogger = Substitute.For<ILogger>();
        var publisher = new TestCompletionQueuePublisher(mockQueue, mockLogger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => publisher.PublishTestCompletion(null!, CancellationToken.None));
    }

    #endregion

    #region TestCaseBatchingService Tests

    /// <summary>
    /// Tests that TestCaseBatchingService can add test cases to batches.
    /// </summary>
    [Fact]
    public async Task TestCaseBatchingService_AddTestCaseToBatch_ShouldCreateBatch()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var batchingService = new TestCaseBatchingService(mockLogger);
        await batchingService.StartAsync();
        var message = CreateTestMessage();

        // Act
        var batchId = await batchingService.AddTestCaseToBatchAsync(message);

        // Assert
        batchId.ShouldNotBeNullOrEmpty();
        var batch = await batchingService.GetBatchAsync(batchId);
        batch.ShouldNotBeNull();
        batch.TestCases.ShouldContain(tc => tc.TestCaseId == message.TestCaseId);
    }

    /// <summary>
    /// Tests that TestCaseBatchingService groups test cases by test class.
    /// </summary>
    [Fact]
    public async Task TestCaseBatchingService_ByTestClassStrategy_ShouldGroupByClass()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var batchingService = new TestCaseBatchingService(mockLogger);
        await batchingService.StartAsync();
        await batchingService.SetBatchingStrategyAsync(BatchingStrategy.ByTestClass);

        var message1 = CreateTestMessage("TestClass1.Test1");
        message1.Metadata["TestClassName"] = "TestClass1";
        var message2 = CreateTestMessage("TestClass1.Test2");
        message2.Metadata["TestClassName"] = "TestClass1";
        var message3 = CreateTestMessage("TestClass2.Test3");
        message3.Metadata["TestClassName"] = "TestClass2";

        // Act
        var batchId1 = await batchingService.AddTestCaseToBatchAsync(message1);
        var batchId2 = await batchingService.AddTestCaseToBatchAsync(message2);
        var batchId3 = await batchingService.AddTestCaseToBatchAsync(message3);

        // Assert
        batchId1.ShouldBe(batchId2); // Same test class should be in same batch
        batchId1.ShouldNotBe(batchId3); // Different test class should be in different batch
        batchId1.ShouldBe("TestClass_TestClass1");
        batchId3.ShouldBe("TestClass_TestClass2");
    }

    #endregion

    #region TestCompletionQueueProcessorBase Tests

    /// <summary>
    /// Mock processor for testing the base class functionality.
    /// </summary>
    private class MockQueueProcessor : TestCompletionQueueProcessorBase
    {
        public bool ProcessCalled { get; private set; }
        public TestCompletionQueueMessage? LastProcessedMessage { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public MockQueueProcessor(ILogger logger) : base(logger) { }

        protected override async Task ProcessTestCompletionCore(TestCompletionQueueMessage message, CancellationToken cancellationToken)
        {
            ProcessCalled = true;
            LastProcessedMessage = message;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Tests that TestCompletionQueueProcessorBase processes messages correctly.
    /// </summary>
    [Fact]
    public async Task TestCompletionQueueProcessorBase_ProcessTestCompletion_ShouldCallCoreMethod()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var processor = new MockQueueProcessor(mockLogger);
        var message = CreateTestMessage();

        // Act
        await processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        processor.ProcessCalled.ShouldBeTrue();
        processor.LastProcessedMessage.ShouldBe(message);
    }

    /// <summary>
    /// Tests that TestCompletionQueueProcessorBase handles exceptions properly.
    /// </summary>
    [Fact]
    public async Task TestCompletionQueueProcessorBase_ProcessWithException_ShouldLogAndRethrow()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var processor = new MockQueueProcessor(mockLogger);
        processor.ExceptionToThrow = new InvalidOperationException("Test exception");
        var message = CreateTestMessage();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => processor.ProcessTestCompletion(message, CancellationToken.None));
    }

    /// <summary>
    /// Tests that TestCompletionQueueProcessorBase throws when message is null.
    /// </summary>
    [Fact]
    public async Task TestCompletionQueueProcessorBase_ProcessNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var processor = new MockQueueProcessor(mockLogger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => processor.ProcessTestCompletion(null!, CancellationToken.None));
    }

    #endregion

    #region FrameworkPublishingProcessor Tests

    /// <summary>
    /// Tests that FrameworkPublishingProcessor publishes framework notifications.
    /// </summary>
    [Fact]
    public async Task FrameworkPublishingProcessor_ProcessTestCompletion_ShouldPublishNotification()
    {
        // Arrange
        var mockMediator = Substitute.For<IMediator>();
        var mockLogger = Substitute.For<ILogger>();
        var processor = new FrameworkPublishingProcessor(mockMediator, mockLogger);
        var message = CreateTestMessage();

        // Act
        await processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Any<FrameworkTestCaseEndNotification>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tests that FrameworkPublishingProcessor handles failed test results.
    /// </summary>
    [Fact]
    public async Task FrameworkPublishingProcessor_ProcessFailedTest_ShouldPublishFailedNotification()
    {
        // Arrange
        var mockMediator = Substitute.For<IMediator>();
        var mockLogger = Substitute.For<ILogger>();
        var processor = new FrameworkPublishingProcessor(mockMediator, mockLogger);
        var message = CreateFailedTestMessage();

        // Act
        await processor.ProcessTestCompletion(message, CancellationToken.None);

        // Assert
        await mockMediator.Received(1).Publish(
            Arg.Is<FrameworkTestCaseEndNotification>(n =>
                n.Exception != null),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region QueueConfiguration Tests

    /// <summary>
    /// Tests that QueueConfiguration has proper default values.
    /// </summary>
    [Fact]
    public void QueueConfiguration_DefaultValues_ShouldBeValid()
    {
        // Act
        var config = new QueueConfiguration();

        // Assert
        config.IsEnabled.ShouldBeFalse(); // Default is false for backward compatibility
        config.MaxQueueCapacity.ShouldBeGreaterThan(0);
        config.ProcessingTimeoutMs.ShouldBeGreaterThan(0);
        config.BatchCompletionTimeoutMs.ShouldBeGreaterThan(0);
        config.MaxRetryAttempts.ShouldBeGreaterThanOrEqualTo(0);
        config.PublishTimeoutMs.ShouldBeGreaterThan(0);
        config.BaseRetryDelayMs.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Tests that QueueConfiguration can be created with valid values.
    /// </summary>
    [Fact]
    public void QueueConfiguration_ValidConfig_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var config = new QueueConfiguration
        {
            IsEnabled = true,
            MaxQueueCapacity = 1000,
            ProcessingTimeoutMs = 5000,
            BatchCompletionTimeoutMs = 10000,
            MaxRetryAttempts = 3,
            PublishTimeoutMs = 2000,
            BaseRetryDelayMs = 500
        };

        // Assert
        config.IsEnabled.ShouldBeTrue();
        config.MaxQueueCapacity.ShouldBe(1000);
        config.ProcessingTimeoutMs.ShouldBe(5000);
        config.BatchCompletionTimeoutMs.ShouldBe(10000);
        config.MaxRetryAttempts.ShouldBe(3);
        config.PublishTimeoutMs.ShouldBe(2000);
        config.BaseRetryDelayMs.ShouldBe(500);
    }

    #endregion

    #region TestCompletionQueueFactory Tests

    /// <summary>
    /// Tests that TestCompletionQueueFactory creates queue instances.
    /// </summary>
    [Fact]
    public async Task TestCompletionQueueFactory_CreateQueue_ShouldReturnQueueInstance()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var factory = new TestCompletionQueueFactory(mockLogger);
        var config = new QueueConfiguration();

        // Act
        var queue = await factory.CreateQueueAsync(config, CancellationToken.None);

        // Assert
        queue.ShouldNotBeNull();
        queue.ShouldBeOfType<InMemoryTestCompletionQueue>();
    }

    /// <summary>
    /// Tests that TestCompletionQueueFactory throws for null configuration.
    /// </summary>
    [Fact]
    public async Task TestCompletionQueueFactory_CreateQueueWithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var factory = new TestCompletionQueueFactory(mockLogger);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => factory.CreateQueueAsync(null!, CancellationToken.None));
    }

    #endregion

    #region Error Handling and Edge Cases

    /// <summary>
    /// Tests that queue operations handle cancellation tokens properly.
    /// </summary>
    [Fact]
    public async Task QueueOperations_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => queue.EnqueueAsync(CreateTestMessage(), cts.Token));
    }

    /// <summary>
    /// Tests that batching service handles concurrent operations safely.
    /// </summary>
    [Fact]
    public async Task TestCaseBatchingService_ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger>();
        var batchingService = new TestCaseBatchingService(mockLogger);
        await batchingService.StartAsync();

        var tasks = new List<Task<string>>();

        // Act - Add multiple test cases concurrently
        for (int i = 0; i < 10; i++)
        {
            var message = CreateTestMessage($"ConcurrentTest{i}");
            tasks.Add(batchingService.AddTestCaseToBatchAsync(message));
        }

        var batchIds = await Task.WhenAll(tasks);

        // Assert
        batchIds.ShouldAllBe(id => !string.IsNullOrEmpty(id));
        batchIds.Distinct().Count().ShouldBeGreaterThan(0); // At least one batch created
    }

    /// <summary>
    /// Tests memory cleanup and disposal patterns.
    /// </summary>
    [Fact]
    public void QueueComponents_Disposal_ShouldCleanupResources()
    {
        // Arrange & Act
        var queue = new InMemoryTestCompletionQueue(1000);
        queue.Dispose();

        // Assert
        queue.IsCompleted.ShouldBeTrue();
        // Note: The actual implementation may not throw ObjectDisposedException for IsRunning
        // but should return false after disposal
        queue.IsRunning.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that QueueDepth property accurately tracks queue depth during enqueue/dequeue operations.
    /// </summary>
    [Fact]
    public async Task InMemoryTestCompletionQueue_QueueDepth_ShouldTrackAccurately()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);

        // Assert initial depth
        queue.QueueDepth.ShouldBe(0);

        // Act - Enqueue first message
        var message1 = CreateTestMessage();
        await queue.EnqueueAsync(message1, CancellationToken.None);

        // Assert depth after first enqueue
        queue.QueueDepth.ShouldBe(1);

        // Act - Enqueue second message
        var message2 = CreateTestMessage();
        await queue.EnqueueAsync(message2, CancellationToken.None);

        // Assert depth after second enqueue
        queue.QueueDepth.ShouldBe(2);

        // Act - Dequeue first message
        var dequeuedMessage1 = await queue.DequeueAsync(CancellationToken.None);

        // Assert depth after first dequeue
        queue.QueueDepth.ShouldBe(1);
        dequeuedMessage1.ShouldNotBeNull();

        // Act - Dequeue second message
        var dequeuedMessage2 = await queue.DequeueAsync(CancellationToken.None);

        // Assert depth after second dequeue
        queue.QueueDepth.ShouldBe(0);
        dequeuedMessage2.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that QueueDepth property works correctly with TryDequeueAsync.
    /// </summary>
    [Fact]
    public async Task InMemoryTestCompletionQueue_QueueDepth_WithTryDequeue_ShouldTrackAccurately()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);

        // Assert initial depth
        queue.QueueDepth.ShouldBe(0);

        // Act - Try dequeue from empty queue
        var emptyResult = await queue.TryDequeueAsync(CancellationToken.None);

        // Assert depth unchanged when no message available
        queue.QueueDepth.ShouldBe(0);
        emptyResult.ShouldBeNull();

        // Act - Enqueue a message
        var message = CreateTestMessage();
        await queue.EnqueueAsync(message, CancellationToken.None);

        // Assert depth after enqueue
        queue.QueueDepth.ShouldBe(1);

        // Act - Try dequeue with message available
        var dequeuedMessage = await queue.TryDequeueAsync(CancellationToken.None);

        // Assert depth after successful try dequeue
        queue.QueueDepth.ShouldBe(0);
        dequeuedMessage.ShouldNotBeNull();
    }

    #endregion

    #region QueueExtensions Tests

    /// <summary>
    /// Tests that GetDiagnosticInfo returns null for Uptime when uptime tracking is not available.
    /// </summary>
    [Fact]
    public async Task GetDiagnosticInfo_ShouldReturnNullUptime()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);

        // Act
        var diagnosticInfo = queue.GetDiagnosticInfo();

        // Assert
        diagnosticInfo.ShouldNotBeNull();
        diagnosticInfo.Uptime.ShouldBeNull();
        diagnosticInfo.Status.ShouldNotBeNull();
        diagnosticInfo.QueueType.ShouldBe("InMemoryTestCompletionQueue");
        diagnosticInfo.AdditionalInfo.ShouldNotBeNull();
        diagnosticInfo.AdditionalInfo.ShouldContainKey("QueueDepth");
        diagnosticInfo.AdditionalInfo.ShouldContainKey("IsRunning");
        diagnosticInfo.AdditionalInfo.ShouldContainKey("IsCompleted");
        diagnosticInfo.AdditionalInfo.ShouldContainKey("IsHealthy");
    }

    /// <summary>
    /// Tests that GetDiagnosticInfo includes correct additional information.
    /// </summary>
    [Fact]
    public async Task GetDiagnosticInfo_ShouldIncludeCorrectAdditionalInfo()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);
        var message = CreateTestMessage();
        await queue.EnqueueAsync(message, CancellationToken.None);

        // Act
        var diagnosticInfo = queue.GetDiagnosticInfo();

        // Assert
        diagnosticInfo.AdditionalInfo["QueueDepth"].ShouldBe(1);
        diagnosticInfo.AdditionalInfo["IsRunning"].ShouldBe(true);
        diagnosticInfo.AdditionalInfo["IsCompleted"].ShouldBe(false);
        diagnosticInfo.AdditionalInfo["IsHealthy"].ShouldBe(true);
    }

    #endregion
}
