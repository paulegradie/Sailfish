using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Extensions;
using Sailfish.TestAdapter.Queue.Implementation;
using Shouldly;
using Xunit;
using ILogger = Sailfish.Logging.ILogger;

namespace Tests.TestAdapter.Queue;

/// <summary>
/// Comprehensive tests for QueueExtensions methods to improve code coverage.
/// </summary>
public class QueueExtensionsTests : IDisposable
{
    private readonly ILogger _logger;

    public QueueExtensionsTests()
    {
        _logger = Substitute.For<ILogger>();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Helper Methods

    private static TestCompletionQueueMessage CreateTestMessage(string testCaseId = "TestCase1")
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
            PerformanceMetrics = new PerformanceMetrics
            {
                MeanMs = 100.0,
                MedianMs = 95.0,
                StandardDeviation = 10.0,
                Variance = 100.0,
                RawExecutionResults = new[] { 90.0, 95.0, 100.0, 105.0, 110.0 },
                DataWithOutliersRemoved = new[] { 95.0, 100.0, 105.0 },
                LowerOutliers = new[] { 90.0 },
                UpperOutliers = new[] { 110.0 },
                TotalNumOutliers = 2,
                SampleSize = 5,
                NumWarmupIterations = 3,
                GroupingId = "Group1"
            },
            Metadata = new Dictionary<string, object>
            {
                ["TestClassName"] = "TestClass1",
                ["TestMethodName"] = "TestMethod1"
            },
            CompletedAt = DateTime.UtcNow
        };
    }

    private static PerformanceMetrics CreatePerformanceMetrics()
    {
        return new PerformanceMetrics
        {
            MeanMs = 50.0,
            MedianMs = 48.0,
            StandardDeviation = 5.0,
            Variance = 25.0,
            RawExecutionResults = new[] { 45.0, 48.0, 50.0, 52.0, 55.0 },
            DataWithOutliersRemoved = new[] { 48.0, 50.0, 52.0 },
            LowerOutliers = new[] { 45.0 },
            UpperOutliers = new[] { 55.0 },
            TotalNumOutliers = 2,
            SampleSize = 5,
            NumWarmupIterations = 2,
            GroupingId = "TestGroup"
        };
    }

    #endregion

    #region EnqueueBatchAsync Tests

    [Fact]
    public async Task EnqueueBatchAsync_WithValidMessages_ShouldEnqueueAll()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);
        
        var messages = new[]
        {
            CreateTestMessage("Test1"),
            CreateTestMessage("Test2"),
            CreateTestMessage("Test3")
        };

        // Act
        await queue.EnqueueBatchAsync(messages, CancellationToken.None);

        // Assert
        queue.QueueDepth.ShouldBe(3);
    }

    [Fact]
    public async Task EnqueueBatchAsync_WithNullQueue_ShouldThrowArgumentNullException()
    {
        // Arrange
        ITestCompletionQueue queue = null!;
        var messages = new[] { CreateTestMessage() };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => 
            queue.EnqueueBatchAsync(messages, CancellationToken.None));
    }

    [Fact]
    public async Task EnqueueBatchAsync_WithNullMessages_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => 
            queue.EnqueueBatchAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task EnqueueBatchAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);
        var messages = new[] { CreateTestMessage() };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => 
            queue.EnqueueBatchAsync(messages, cts.Token));
    }

    #endregion

    #region IsHealthy Tests

    [Fact]
    public async Task IsHealthy_WhenRunningAndNotCompleted_ShouldReturnTrue()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);

        // Act
        var isHealthy = queue.IsHealthy();

        // Assert
        isHealthy.ShouldBeTrue();
    }

    [Fact]
    public void IsHealthy_WhenNotRunning_ShouldReturnFalse()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        // Don't start the queue

        // Act
        var isHealthy = queue.IsHealthy();

        // Assert
        isHealthy.ShouldBeFalse();
    }

    [Fact]
    public async Task IsHealthy_WhenCompleted_ShouldReturnFalse()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);
        await queue.CompleteAsync(CancellationToken.None);

        // Act
        var isHealthy = queue.IsHealthy();

        // Assert
        isHealthy.ShouldBeFalse();
    }

    [Fact]
    public void IsHealthy_WithNullQueue_ShouldThrowArgumentNullException()
    {
        // Arrange
        ITestCompletionQueue queue = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => queue.IsHealthy());
    }

    #endregion

    #region GetQueueStatus Tests

    [Fact]
    public async Task GetQueueStatus_WithRunningQueue_ShouldReturnCorrectStatus()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);
        var message = CreateTestMessage();
        await queue.EnqueueAsync(message, CancellationToken.None);

        // Act
        var status = queue.GetQueueStatus();

        // Assert
        status.ShouldNotBeNull();
        status.IsRunning.ShouldBeTrue();
        status.IsCompleted.ShouldBeFalse();
        status.QueueDepth.ShouldBe(1);
        status.StatusTimestamp.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public void GetQueueStatus_WithNullQueue_ShouldThrowArgumentNullException()
    {
        // Arrange
        ITestCompletionQueue queue = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => queue.GetQueueStatus());
    }

    #endregion

    #region WaitForEmptyAsync Tests

    [Fact]
    public async Task WaitForEmptyAsync_WithEmptyQueue_ShouldReturnTrueImmediately()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);

        // Act
        var result = await queue.WaitForEmptyAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task WaitForEmptyAsync_WithTimeout_ShouldReturnFalse()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);
        await queue.EnqueueAsync(CreateTestMessage(), CancellationToken.None);

        // Act
        var result = await queue.WaitForEmptyAsync(TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task WaitForEmptyAsync_WithNullQueue_ShouldThrowArgumentNullException()
    {
        // Arrange
        ITestCompletionQueue queue = null!;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => 
            queue.WaitForEmptyAsync(TimeSpan.FromSeconds(1), CancellationToken.None));
    }

    #endregion

    #region Message Creation Tests

    [Fact]
    public void CreateMessage_WithValidParameters_ShouldCreateMessage()
    {
        // Arrange
        var testCaseId = "TestCase1";
        var testResult = new TestExecutionResult { IsSuccess = true };
        var performanceMetrics = CreatePerformanceMetrics();
        var metadata = new Dictionary<string, object> { ["Key1"] = "Value1" };

        // Act
        var message = QueueExtensions.CreateMessage(testCaseId, testResult, performanceMetrics, metadata);

        // Assert
        message.ShouldNotBeNull();
        message.TestCaseId.ShouldBe(testCaseId);
        message.TestResult.ShouldBe(testResult);
        message.PerformanceMetrics.ShouldBe(performanceMetrics);
        message.Metadata.ShouldBe(metadata);
        message.CompletedAt.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public void CreateMessage_WithNullTestCaseId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var testResult = new TestExecutionResult { IsSuccess = true };
        var performanceMetrics = CreatePerformanceMetrics();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            QueueExtensions.CreateMessage(null!, testResult, performanceMetrics));
    }

    [Fact]
    public void CreateSuccessMessage_WithValidParameters_ShouldCreateSuccessMessage()
    {
        // Arrange
        var testCaseId = "TestCase1";
        var performanceMetrics = CreatePerformanceMetrics();
        var metadata = new Dictionary<string, object> { ["Key1"] = "Value1" };

        // Act
        var message = QueueExtensions.CreateSuccessMessage(testCaseId, performanceMetrics, metadata);

        // Assert
        message.ShouldNotBeNull();
        message.TestCaseId.ShouldBe(testCaseId);
        message.TestResult.IsSuccess.ShouldBeTrue();
        message.PerformanceMetrics.ShouldBe(performanceMetrics);
        message.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public void CreateFailureMessage_WithValidParameters_ShouldCreateFailureMessage()
    {
        // Arrange
        var testCaseId = "TestCase1";
        var exception = new InvalidOperationException("Test error");
        var performanceMetrics = CreatePerformanceMetrics();
        var metadata = new Dictionary<string, object> { ["Key1"] = "Value1" };

        // Act
        var message = QueueExtensions.CreateFailureMessage(testCaseId, exception, performanceMetrics, metadata);

        // Assert
        message.ShouldNotBeNull();
        message.TestCaseId.ShouldBe(testCaseId);
        message.TestResult.IsSuccess.ShouldBeFalse();
        message.TestResult.ExceptionMessage.ShouldBe(exception.Message);
        message.PerformanceMetrics.ShouldBe(performanceMetrics);
        message.Metadata.ShouldBe(metadata);
    }

    #endregion

    #region Message Fluent Extensions Tests

    [Fact]
    public void WithMetadata_WithValidKeyValue_ShouldAddMetadata()
    {
        // Arrange
        var message = CreateTestMessage();
        var key = "NewKey";
        var value = "NewValue";

        // Act
        var result = message.WithMetadata(key, value);

        // Assert
        result.ShouldBe(message); // Should return same instance
        message.Metadata.ShouldContainKeyAndValue(key, value);
    }

    [Fact]
    public void WithMetadata_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestCompletionQueueMessage message = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => message.WithMetadata("key", "value"));
    }

    [Fact]
    public void WithMetadata_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => message.WithMetadata(null!, "value"));
    }

    [Fact]
    public void WithGroupingId_WithValidId_ShouldSetGroupingId()
    {
        // Arrange
        var message = CreateTestMessage();
        var groupingId = "NewGroupId";

        // Act
        var result = message.WithGroupingId(groupingId);

        // Assert
        result.ShouldBe(message); // Should return same instance
        message.PerformanceMetrics.GroupingId.ShouldBe(groupingId);
    }

    [Fact]
    public void WithGroupingId_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestCompletionQueueMessage message = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => message.WithGroupingId("groupId"));
    }

    [Fact]
    public void WithGroupingId_WithNullGroupingId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var message = CreateTestMessage();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => message.WithGroupingId(null!));
    }

    #endregion

    #region Publisher Extensions Tests

    [Fact]
    public async Task PublishBatchAsync_WithValidMessages_ShouldPublishAll()
    {
        // Arrange
        var publisher = Substitute.For<ITestCompletionQueuePublisher>();
        var messages = new[]
        {
            CreateTestMessage("Test1"),
            CreateTestMessage("Test2"),
            CreateTestMessage("Test3")
        };

        // Act
        await publisher.PublishBatchAsync(messages, CancellationToken.None);

        // Assert
        await publisher.Received(3).PublishTestCompletion(Arg.Any<TestCompletionQueueMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBatchAsync_WithNullPublisher_ShouldThrowArgumentNullException()
    {
        // Arrange
        ITestCompletionQueuePublisher publisher = null!;
        var messages = new[] { CreateTestMessage() };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            publisher.PublishBatchAsync(messages, CancellationToken.None));
    }

    [Fact]
    public async Task PublishWithRetryAsync_WithSuccessfulPublish_ShouldPublishOnce()
    {
        // Arrange
        var publisher = Substitute.For<ITestCompletionQueuePublisher>();
        var message = CreateTestMessage();
        publisher.PublishTestCompletion(message, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        await publisher.PublishWithRetryAsync(message, maxRetries: 3, delay: TimeSpan.FromMilliseconds(10));

        // Assert
        await publisher.Received(1).PublishTestCompletion(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishWithRetryAsync_WithFailures_ShouldRetryAndEventuallySucceed()
    {
        // Arrange
        var publisher = Substitute.For<ITestCompletionQueuePublisher>();
        var message = CreateTestMessage();
        publisher.PublishTestCompletion(message, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException(new InvalidOperationException("First failure")),
                Task.FromException(new InvalidOperationException("Second failure")),
                Task.CompletedTask); // Third attempt succeeds

        // Act
        await publisher.PublishWithRetryAsync(message, maxRetries: 3, delay: TimeSpan.FromMilliseconds(10));

        // Assert
        await publisher.Received(3).PublishTestCompletion(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishWithRetryAsync_WithAllFailures_ShouldThrowLastException()
    {
        // Arrange
        var publisher = Substitute.For<ITestCompletionQueuePublisher>();
        var message = CreateTestMessage();
        var lastException = new InvalidOperationException("Final failure");
        publisher.PublishTestCompletion(message, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException(new InvalidOperationException("First failure")),
                Task.FromException(new InvalidOperationException("Second failure")),
                Task.FromException(lastException));

        // Act & Assert
        var thrownException = await Should.ThrowAsync<InvalidOperationException>(() =>
            publisher.PublishWithRetryAsync(message, maxRetries: 2, delay: TimeSpan.FromMilliseconds(10)));

        thrownException.ShouldBe(lastException);
        await publisher.Received(3).PublishTestCompletion(message, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Logging and Health Check Extensions Tests

    [Fact]
    public async Task LogQueueStatus_WithHealthyQueue_ShouldLogInfo()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);

        // Act
        queue.LogQueueStatus(_logger);

        // Assert
        _logger.Received().Log(Arg.Any<Sailfish.Logging.LogLevel>(), Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public void LogQueueStatus_WithNullQueue_ShouldThrowArgumentNullException()
    {
        // Arrange
        ITestCompletionQueue queue = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => queue.LogQueueStatus(_logger));
    }

    [Fact]
    public async Task ToHealthCheckResult_WithHealthyQueue_ShouldReturnHealthyResult()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        await queue.StartAsync(CancellationToken.None);

        // Act
        var result = queue.ToHealthCheckResult();

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe("Healthy");
        result.IsHealthy.ShouldBeTrue();
        result.Details.ShouldNotBeNull();
        result.Details.ShouldContainKey("QueueDepth");
        result.Details.ShouldContainKey("IsRunning");
        result.Details.ShouldContainKey("IsCompleted");
        result.Details.ShouldContainKey("Timestamp");
        var timestamp = (DateTime)result.Details["Timestamp"];
        timestamp.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public void ToHealthCheckResult_WithUnhealthyQueue_ShouldReturnUnhealthyResult()
    {
        // Arrange
        using var queue = new InMemoryTestCompletionQueue(1000);
        // Don't start the queue

        // Act
        var result = queue.ToHealthCheckResult();

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe("Unhealthy");
        result.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    public void ToHealthCheckResult_WithNullQueue_ShouldThrowArgumentNullException()
    {
        // Arrange
        ITestCompletionQueue queue = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => queue.ToHealthCheckResult());
    }

    #endregion
}
