using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Extensions;

/// <summary>
/// Provides extension methods for queue operations, message creation helpers, and utility methods
/// for monitoring and diagnostics. These extensions simplify common queue operations and provide
/// convenient helpers for working with the intercepting queue architecture.
/// </summary>
/// <remarks>
/// The QueueExtensions class contains extension methods that enhance the usability of the
/// queue system by providing:
/// 
/// - Simplified queue operations for batch processing and status monitoring
/// - Factory methods and fluent builders for message creation
/// - Diagnostic and monitoring utilities for queue health assessment
/// - Integration helpers for logging and error handling
/// - Retry mechanisms and timeout support for reliability
/// 
/// All extension methods are designed to be thread-safe and support async/await patterns
/// with proper cancellation token handling. The methods follow the established patterns
/// in the Sailfish Test Adapter for error handling, logging, and documentation.
/// </remarks>
public static class QueueExtensions
{
    #region Queue Operation Extensions

    /// <summary>
    /// Enqueues multiple test completion messages to the queue in a batch operation.
    /// </summary>
    /// <param name="queue">The queue to enqueue messages to.</param>
    /// <param name="messages">The collection of test completion messages to enqueue.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous batch enqueue operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> or <paramref name="messages"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides a convenient way to enqueue multiple messages efficiently.
    /// Each message is enqueued individually, but the operation is optimized for batch scenarios.
    /// If any individual enqueue operation fails, the method will stop processing and throw the exception.
    /// </remarks>
    public static async Task EnqueueBatchAsync(
        this ITestCompletionQueue queue,
        IEnumerable<TestCompletionQueueMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(messages);

        foreach (var message in messages)
        {
            await queue.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Checks if the queue is in a healthy state and operational.
    /// </summary>
    /// <param name="queue">The queue to check.</param>
    /// <returns><c>true</c> if the queue is healthy and operational; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> is null.
    /// </exception>
    /// <remarks>
    /// A queue is considered healthy if it is running and not in a completed state.
    /// This is a quick health check that can be used for monitoring purposes.
    /// </remarks>
    public static bool IsHealthy(this ITestCompletionQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        return queue.IsRunning && !queue.IsCompleted;
    }

    /// <summary>
    /// Gets comprehensive status information about the queue.
    /// </summary>
    /// <param name="queue">The queue to get status for.</param>
    /// <returns>A <see cref="QueueStatus"/> record containing current queue status information.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> is null.
    /// </exception>
    /// <remarks>
    /// This method provides a snapshot of the queue's current state including
    /// running status, completion status, queue depth, and timestamp.
    /// </remarks>
    public static QueueStatus GetQueueStatus(this ITestCompletionQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        return new QueueStatus(
            queue.IsRunning,
            queue.IsCompleted,
            queue.QueueDepth,
            DateTime.UtcNow
        );
    }

    /// <summary>
    /// Waits for the queue to become empty within the specified timeout period.
    /// </summary>
    /// <param name="queue">The queue to wait for.</param>
    /// <param name="timeout">The maximum time to wait for the queue to become empty.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous wait operation.
    /// Returns <c>true</c> if the queue became empty within the timeout; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is useful for waiting for queue processing to complete before
    /// shutting down or performing cleanup operations. It polls the queue depth
    /// at regular intervals until the queue is empty or the timeout expires.
    /// </remarks>
    public static async Task<bool> WaitForEmptyAsync(
        this ITestCompletionQueue queue,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queue);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            while (queue.QueueDepth > 0 && !combinedCts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), combinedCts.Token).ConfigureAwait(false);
            }
            return queue.QueueDepth == 0;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            return false; // Timeout occurred
        }
    }

    #endregion

    #region Message Creation Helpers

    /// <summary>
    /// Creates a new test completion queue message with the specified parameters.
    /// </summary>
    /// <param name="testCaseId">The unique identifier for the test case.</param>
    /// <param name="testResult">The test execution result information.</param>
    /// <param name="performanceMetrics">The performance metrics collected during test execution.</param>
    /// <param name="metadata">Optional additional metadata associated with the test execution.</param>
    /// <returns>A new <see cref="TestCompletionQueueMessage"/> instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="testCaseId"/>, <paramref name="testResult"/>, 
    /// or <paramref name="performanceMetrics"/> is null.
    /// </exception>
    /// <remarks>
    /// This factory method provides a convenient way to create test completion messages
    /// with all required properties. The CompletedAt timestamp is automatically set to the current UTC time.
    /// </remarks>
    public static TestCompletionQueueMessage CreateMessage(
        string testCaseId,
        TestExecutionResult testResult,
        PerformanceMetrics performanceMetrics,
        Dictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(testCaseId);
        ArgumentNullException.ThrowIfNull(testResult);
        ArgumentNullException.ThrowIfNull(performanceMetrics);

        return new TestCompletionQueueMessage
        {
            TestCaseId = testCaseId,
            TestResult = testResult,
            PerformanceMetrics = performanceMetrics,
            CompletedAt = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Creates a test completion queue message for a successful test execution.
    /// </summary>
    /// <param name="testCaseId">The unique identifier for the test case.</param>
    /// <param name="performanceMetrics">The performance metrics collected during test execution.</param>
    /// <param name="metadata">Optional additional metadata associated with the test execution.</param>
    /// <returns>A new <see cref="TestCompletionQueueMessage"/> instance for a successful test.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="testCaseId"/> or <paramref name="performanceMetrics"/> is null.
    /// </exception>
    /// <remarks>
    /// This is a convenience method for creating messages for successful test executions.
    /// The TestResult is automatically configured with IsSuccess = true.
    /// </remarks>
    public static TestCompletionQueueMessage CreateSuccessMessage(
        string testCaseId,
        PerformanceMetrics performanceMetrics,
        Dictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(testCaseId);
        ArgumentNullException.ThrowIfNull(performanceMetrics);

        var testResult = new TestExecutionResult { IsSuccess = true };
        return CreateMessage(testCaseId, testResult, performanceMetrics, metadata);
    }

    /// <summary>
    /// Creates a test completion queue message for a failed test execution.
    /// </summary>
    /// <param name="testCaseId">The unique identifier for the test case.</param>
    /// <param name="exception">The exception that caused the test failure.</param>
    /// <param name="performanceMetrics">Optional performance metrics collected before the failure.</param>
    /// <param name="metadata">Optional additional metadata associated with the test execution.</param>
    /// <returns>A new <see cref="TestCompletionQueueMessage"/> instance for a failed test.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="testCaseId"/> or <paramref name="exception"/> is null.
    /// </exception>
    /// <remarks>
    /// This is a convenience method for creating messages for failed test executions.
    /// The TestResult is automatically configured with failure information from the exception.
    /// If no performance metrics are provided, an empty PerformanceMetrics instance is used.
    /// </remarks>
    public static TestCompletionQueueMessage CreateFailureMessage(
        string testCaseId,
        Exception exception,
        PerformanceMetrics? performanceMetrics = null,
        Dictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(testCaseId);
        ArgumentNullException.ThrowIfNull(exception);

        var testResult = new TestExecutionResult
        {
            IsSuccess = false,
            ExceptionMessage = exception.Message,
            ExceptionDetails = exception.ToString(),
            ExceptionType = exception.GetType().Name
        };

        return CreateMessage(testCaseId, testResult, performanceMetrics ?? new PerformanceMetrics(), metadata);
    }

    /// <summary>
    /// Adds metadata to an existing test completion queue message using a fluent interface.
    /// </summary>
    /// <param name="message">The message to add metadata to.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The same message instance with the metadata added.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message"/>, <paramref name="key"/>, or <paramref name="value"/> is null.
    /// </exception>
    /// <remarks>
    /// This extension method enables fluent-style metadata addition to messages.
    /// If the key already exists, the value will be overwritten.
    /// </remarks>
    public static TestCompletionQueueMessage WithMetadata(
        this TestCompletionQueueMessage message,
        string key,
        object value)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        message.Metadata[key] = value;
        return message;
    }

    /// <summary>
    /// Sets the grouping identifier for batch processing using a fluent interface.
    /// </summary>
    /// <param name="message">The message to set the grouping identifier for.</param>
    /// <param name="groupingId">The grouping identifier for batch processing.</param>
    /// <returns>The same message instance with the grouping identifier set.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message"/> or <paramref name="groupingId"/> is null.
    /// </exception>
    /// <remarks>
    /// This extension method enables fluent-style grouping identifier assignment.
    /// The grouping identifier is used to group related test cases for comparison analysis.
    /// </remarks>
    public static TestCompletionQueueMessage WithGroupingId(
        this TestCompletionQueueMessage message,
        string groupingId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(groupingId);

        message.PerformanceMetrics.GroupingId = groupingId;
        return message;
    }

    #endregion

    #region Publisher Extensions

    /// <summary>
    /// Publishes multiple test completion messages to the queue in a batch operation.
    /// </summary>
    /// <param name="publisher">The publisher to use for publishing messages.</param>
    /// <param name="messages">The collection of test completion messages to publish.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous batch publish operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="publisher"/> or <paramref name="messages"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides a convenient way to publish multiple messages efficiently.
    /// Each message is published individually, but the operation is optimized for batch scenarios.
    /// If any individual publish operation fails, the method will stop processing and throw the exception.
    /// </remarks>
    public static async Task PublishBatchAsync(
        this ITestCompletionQueuePublisher publisher,
        IEnumerable<TestCompletionQueueMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(messages);

        foreach (var message in messages)
        {
            await publisher.PublishTestCompletion(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Publishes a test completion message with retry logic for improved reliability.
    /// </summary>
    /// <param name="publisher">The publisher to use for publishing the message.</param>
    /// <param name="message">The test completion message to publish.</param>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <param name="delay">The delay between retry attempts.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous publish operation with retry logic.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="publisher"/> or <paramref name="message"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxRetries"/> is negative.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method attempts to publish the message with exponential backoff retry logic.
    /// If all retry attempts fail, the last exception is thrown. The delay between retries
    /// increases exponentially to avoid overwhelming the queue system.
    /// </remarks>
    public static async Task PublishWithRetryAsync(
        this ITestCompletionQueuePublisher publisher,
        TestCompletionQueueMessage message,
        int maxRetries = 3,
        TimeSpan? delay = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        var retryDelay = delay ?? TimeSpan.FromMilliseconds(100);
        Exception? lastException = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await publisher.PublishTestCompletion(message, cancellationToken).ConfigureAwait(false);
                return; // Success
            }
            catch (OperationCanceledException)
            {
                throw; // Don't retry on cancellation
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxRetries)
                {
                    var currentDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * Math.Pow(2, attempt));
                    await Task.Delay(currentDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        throw lastException ?? new InvalidOperationException("Publish operation failed after all retry attempts.");
    }

    #endregion

    #region Monitoring and Diagnostics Extensions

    /// <summary>
    /// Gets comprehensive diagnostic information about the queue.
    /// </summary>
    /// <param name="queue">The queue to get diagnostic information for.</param>
    /// <returns>A <see cref="QueueDiagnosticInfo"/> record containing diagnostic information.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> is null.
    /// </exception>
    /// <remarks>
    /// This method provides detailed diagnostic information about the queue including
    /// status, queue type, and additional diagnostic details. The uptime field will be
    /// null when uptime tracking is not available, which requires queue implementations
    /// to track their start time.
    /// </remarks>
    public static QueueDiagnosticInfo GetDiagnosticInfo(this ITestCompletionQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);

        var status = queue.GetQueueStatus();
        var additionalInfo = new Dictionary<string, object>
        {
            ["QueueDepth"] = queue.QueueDepth,
            ["IsRunning"] = queue.IsRunning,
            ["IsCompleted"] = queue.IsCompleted,
            ["IsHealthy"] = queue.IsHealthy()
        };

        return new QueueDiagnosticInfo(
            status,
            null, // Uptime is not available without start time tracking
            queue.GetType().Name,
            additionalInfo
        );
    }

    /// <summary>
    /// Logs the current queue status using the provided logger.
    /// </summary>
    /// <param name="queue">The queue to log status for.</param>
    /// <param name="logger">The logger to use for logging.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> or <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// This method provides a convenient way to log queue status information
    /// using the Sailfish logging infrastructure. The log level is automatically
    /// determined based on the queue's health status.
    /// </remarks>
    public static void LogQueueStatus(this ITestCompletionQueue queue, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(logger);

        var status = queue.GetQueueStatus();
        var isHealthy = queue.IsHealthy();

        if (isHealthy)
        {
            logger.Log(LogLevel.Information,
                "Queue Status: Running={IsRunning}, Completed={IsCompleted}, Depth={QueueDepth}, Healthy={IsHealthy}",
                status.IsRunning, status.IsCompleted, status.QueueDepth, isHealthy);
        }
        else
        {
            logger.Log(LogLevel.Warning,
                "Queue Status: Running={IsRunning}, Completed={IsCompleted}, Depth={QueueDepth}, Healthy={IsHealthy}",
                status.IsRunning, status.IsCompleted, status.QueueDepth, isHealthy);
        }
    }

    /// <summary>
    /// Creates a health check result for the queue.
    /// </summary>
    /// <param name="queue">The queue to create a health check result for.</param>
    /// <returns>A <see cref="QueueHealthCheckResult"/> record containing health check information.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> is null.
    /// </exception>
    /// <remarks>
    /// This method provides a standardized health check result that can be used
    /// for monitoring and alerting systems. The health status is determined based
    /// on the queue's operational state.
    /// </remarks>
    public static QueueHealthCheckResult ToHealthCheckResult(this ITestCompletionQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);

        var isHealthy = queue.IsHealthy();
        var status = isHealthy ? "Healthy" : "Unhealthy";
        var details = new Dictionary<string, object>
        {
            ["IsRunning"] = queue.IsRunning,
            ["IsCompleted"] = queue.IsCompleted,
            ["QueueDepth"] = queue.QueueDepth,
            ["Timestamp"] = DateTime.UtcNow
        };

        return new QueueHealthCheckResult(isHealthy, status, details);
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Represents the current status of a test completion queue.
/// </summary>
/// <remarks>
/// This record provides a snapshot of the queue's operational state at a specific point in time.
/// It is used by monitoring and diagnostic operations to assess queue health and performance.
/// </remarks>
public record QueueStatus
{
    /// <summary>
    /// Represents the current status of a test completion queue.
    /// </summary>
    /// <param name="IsRunning">Indicates whether the queue is currently running and operational.</param>
    /// <param name="IsCompleted">Indicates whether the queue has been marked as complete.</param>
    /// <param name="QueueDepth">The current number of messages waiting in the queue.</param>
    /// <param name="StatusTimestamp">The timestamp when this status was captured.</param>
    /// <remarks>
    /// This record provides a snapshot of the queue's operational state at a specific point in time.
    /// It is used by monitoring and diagnostic operations to assess queue health and performance.
    /// </remarks>
    public QueueStatus(bool IsRunning,
        bool IsCompleted,
        int QueueDepth,
        DateTime StatusTimestamp)
    {
        this.IsRunning = IsRunning;
        this.IsCompleted = IsCompleted;
        this.QueueDepth = QueueDepth;
        this.StatusTimestamp = StatusTimestamp;
    }

    /// <summary>Indicates whether the queue is currently running and operational.</summary>
    public bool IsRunning { get; init; }

    /// <summary>Indicates whether the queue has been marked as complete.</summary>
    public bool IsCompleted { get; init; }

    /// <summary>The current number of messages waiting in the queue.</summary>
    public int QueueDepth { get; init; }

    /// <summary>The timestamp when this status was captured.</summary>
    public DateTime StatusTimestamp { get; init; }

    public void Deconstruct(out bool IsRunning, out bool IsCompleted, out int QueueDepth, out DateTime StatusTimestamp)
    {
        IsRunning = this.IsRunning;
        IsCompleted = this.IsCompleted;
        QueueDepth = this.QueueDepth;
        StatusTimestamp = this.StatusTimestamp;
    }
}

/// <summary>
/// Represents comprehensive diagnostic information about a test completion queue.
/// </summary>
/// <remarks>
/// This record provides detailed diagnostic information that can be used for troubleshooting,
/// monitoring, and performance analysis of the queue system. The additional information
/// dictionary can contain implementation-specific diagnostic data. The Uptime field will be
/// null when the queue implementation does not track start time for uptime calculation.
/// </remarks>
public record QueueDiagnosticInfo
{
    /// <summary>
    /// Represents comprehensive diagnostic information about a test completion queue.
    /// </summary>
    /// <param name="Status">The current status of the queue.</param>
    /// <param name="Uptime">The uptime of the queue service, or null if uptime tracking is not available.</param>
    /// <param name="QueueType">The type name of the queue implementation.</param>
    /// <param name="AdditionalInfo">Additional diagnostic information specific to the queue implementation.</param>
    /// <remarks>
    /// This record provides detailed diagnostic information that can be used for troubleshooting,
    /// monitoring, and performance analysis of the queue system. The additional information
    /// dictionary can contain implementation-specific diagnostic data. The Uptime field will be
    /// null when the queue implementation does not track start time for uptime calculation.
    /// </remarks>
    public QueueDiagnosticInfo(QueueStatus Status,
        TimeSpan? Uptime,
        string QueueType,
        Dictionary<string, object> AdditionalInfo)
    {
        this.Status = Status;
        this.Uptime = Uptime;
        this.QueueType = QueueType;
        this.AdditionalInfo = AdditionalInfo;
    }

    /// <summary>The current status of the queue.</summary>
    public QueueStatus Status { get; init; }

    /// <summary>The uptime of the queue service, or null if uptime tracking is not available.</summary>
    public TimeSpan? Uptime { get; init; }

    /// <summary>The type name of the queue implementation.</summary>
    public string QueueType { get; init; }

    /// <summary>Additional diagnostic information specific to the queue implementation.</summary>
    public Dictionary<string, object> AdditionalInfo { get; init; }

    public void Deconstruct(out QueueStatus Status, out TimeSpan? Uptime, out string QueueType, out Dictionary<string, object> AdditionalInfo)
    {
        Status = this.Status;
        Uptime = this.Uptime;
        QueueType = this.QueueType;
        AdditionalInfo = this.AdditionalInfo;
    }
}

/// <summary>
/// Represents the result of a health check operation on a test completion queue.
/// </summary>
/// <remarks>
/// This record provides a standardized format for health check results that can be used
/// by monitoring systems, alerting mechanisms, and diagnostic tools. The details dictionary
/// contains specific information about the queue's operational state.
/// </remarks>
public record QueueHealthCheckResult
{
    /// <summary>
    /// Represents the result of a health check operation on a test completion queue.
    /// </summary>
    /// <param name="IsHealthy">Indicates whether the queue is in a healthy state.</param>
    /// <param name="Status">A human-readable status description.</param>
    /// <param name="Details">Additional details about the health check result.</param>
    /// <remarks>
    /// This record provides a standardized format for health check results that can be used
    /// by monitoring systems, alerting mechanisms, and diagnostic tools. The details dictionary
    /// contains specific information about the queue's operational state.
    /// </remarks>
    public QueueHealthCheckResult(bool IsHealthy,
        string Status,
        Dictionary<string, object> Details)
    {
        this.IsHealthy = IsHealthy;
        this.Status = Status;
        this.Details = Details;
    }

    /// <summary>Indicates whether the queue is in a healthy state.</summary>
    public bool IsHealthy { get; init; }

    /// <summary>A human-readable status description.</summary>
    public string Status { get; init; }

    /// <summary>Additional details about the health check result.</summary>
    public Dictionary<string, object> Details { get; init; }

    public void Deconstruct(out bool IsHealthy, out string Status, out Dictionary<string, object> Details)
    {
        IsHealthy = this.IsHealthy;
        Status = this.Status;
        Details = this.Details;
    }
}

#endregion
