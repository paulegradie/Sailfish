using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for comprehensive queue metrics collection and monitoring.
/// This interface provides methods for tracking queue performance, processing statistics,
/// and operational metrics across the entire queue system lifecycle.
/// </summary>
/// <remarks>
/// The IQueueMetrics interface enables centralized metrics collection for the intercepting
/// queue architecture, providing detailed tracking of queue operations, processing performance,
/// batch statistics, and system health indicators. This service complements the health check
/// system by providing historical metrics tracking and comprehensive performance analysis.
/// 
/// Key responsibilities:
/// - Track messages published, processed, and failed across all queue operations
/// - Monitor queue depth changes over time with historical data retention
/// - Collect batch completion rates and timeout statistics for batch processing analysis
/// - Calculate processing rates and latencies for performance monitoring
/// - Expose comprehensive metrics for external monitoring and analysis systems
/// - Provide thread-safe metrics collection for concurrent queue operations
/// 
/// The metrics service integrates with all queue components including publishers, consumers,
/// processors, and batch services to provide a complete view of queue system performance.
/// 
/// Thread Safety:
/// All methods in this interface must be thread-safe and support concurrent access from
/// multiple threads. Implementations should use appropriate synchronization mechanisms
/// to ensure data consistency and prevent race conditions.
/// 
/// Integration:
/// This service is designed to be injected into queue components that need to report
/// metrics, and to be queried by monitoring and analysis systems that need access to
/// comprehensive queue performance data.
/// </remarks>
public interface IQueueMetrics
{
    /// <summary>
    /// Records that a message was published to the queue.
    /// </summary>
    /// <param name="testCaseId">The unique identifier of the test case.</param>
    /// <param name="timestamp">The timestamp when the message was published.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="testCaseId"/> is null or empty.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method should be called by queue publishers when a test completion message
    /// is successfully added to the queue. The timestamp should reflect when the
    /// publishing operation completed.
    /// </remarks>
    void RecordMessagePublished(string testCaseId, DateTime timestamp);

    /// <summary>
    /// Records that a message was successfully processed by a queue processor.
    /// </summary>
    /// <param name="testCaseId">The unique identifier of the test case.</param>
    /// <param name="processorName">The name of the processor that handled the message.</param>
    /// <param name="processingTimeMs">The time taken to process the message in milliseconds.</param>
    /// <param name="timestamp">The timestamp when processing completed.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="testCaseId"/> or <paramref name="processorName"/> is null or empty.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method should be called by queue processors when they successfully complete
    /// processing of a test completion message. The processing time should include the
    /// total time spent in the processor's ProcessTestCompletion method.
    /// </remarks>
    void RecordMessageProcessed(string testCaseId, string processorName, double processingTimeMs, DateTime timestamp);

    /// <summary>
    /// Records that a message failed processing.
    /// </summary>
    /// <param name="testCaseId">The unique identifier of the test case.</param>
    /// <param name="processorName">The name of the processor that failed.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="timestamp">The timestamp when the failure occurred.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="testCaseId"/> or <paramref name="processorName"/> is null or empty.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method should be called by queue processors when they encounter an error
    /// during message processing. This includes both transient errors that may be
    /// retried and permanent failures.
    /// </remarks>
    void RecordMessageFailed(string testCaseId, string processorName, string errorMessage, DateTime timestamp);

    /// <summary>
    /// Records the current queue depth for monitoring queue size over time.
    /// </summary>
    /// <param name="depth">The current number of messages in the queue.</param>
    /// <param name="timestamp">The timestamp when the depth was measured.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="depth"/> is negative.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method should be called periodically by queue implementations to track
    /// queue depth changes over time. The frequency of calls should balance between
    /// providing useful trend data and minimizing performance impact.
    /// </remarks>
    void RecordQueueDepth(int depth, DateTime timestamp);

    /// <summary>
    /// Records batch completion statistics.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch.</param>
    /// <param name="batchSize">The number of test cases in the batch.</param>
    /// <param name="completedSuccessfully">Whether the batch completed successfully.</param>
    /// <param name="processingTimeMs">The total time to process the batch in milliseconds.</param>
    /// <param name="timestamp">The timestamp when the batch completed.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="batchId"/> is null or empty, or when <paramref name="batchSize"/> is negative.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method should be called by batch processing services when a batch of
    /// test cases completes processing, either successfully or with failures.
    /// The processing time should include the total time from batch creation to completion.
    /// </remarks>
    void RecordBatchCompletion(string batchId, int batchSize, bool completedSuccessfully, double processingTimeMs, DateTime timestamp);

    /// <summary>
    /// Records batch timeout events.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch that timed out.</param>
    /// <param name="expectedSize">The expected number of test cases in the batch.</param>
    /// <param name="actualSize">The actual number of test cases received before timeout.</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    /// <param name="timestamp">The timestamp when the timeout occurred.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="batchId"/> is null or empty.
    /// </exception>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method should be called by batch timeout handlers when a batch fails
    /// to complete within the configured timeout period. This helps track batch
    /// completion reliability and timeout frequency.
    /// </remarks>
    void RecordBatchTimeout(string batchId, int expectedSize, int actualSize, double timeoutMs, DateTime timestamp);

    /// <summary>
    /// Gets comprehensive queue metrics for the specified time period.
    /// </summary>
    /// <param name="fromTime">The start time for metrics collection (null for all time).</param>
    /// <param name="toTime">The end time for metrics collection (null for current time).</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains comprehensive metrics for the specified time period.
    /// </returns>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method provides a comprehensive snapshot of all queue metrics for the
    /// specified time period. If no time period is specified, it returns metrics
    /// for the entire lifetime of the metrics service.
    /// </remarks>
    Task<QueueMetricsSnapshot> GetMetricsAsync(DateTime? fromTime = null, DateTime? toTime = null);

    /// <summary>
    /// Gets current processing rates and performance statistics.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains current processing rates and performance metrics.
    /// </returns>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method provides real-time processing rate information including current
    /// throughput, latency measurements, and trend indicators for performance monitoring.
    /// </remarks>
    Task<ProcessingRateMetrics> GetProcessingRatesAsync();

    /// <summary>
    /// Gets queue depth statistics over time.
    /// </summary>
    /// <param name="fromTime">The start time for depth analysis (null for all time).</param>
    /// <param name="toTime">The end time for depth analysis (null for current time).</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains queue depth statistics and trends.
    /// </returns>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method provides detailed analysis of queue depth changes over time,
    /// including average depth, peak usage, and trend analysis for capacity planning.
    /// </remarks>
    Task<QueueDepthMetrics> GetQueueDepthMetricsAsync(DateTime? fromTime = null, DateTime? toTime = null);

    /// <summary>
    /// Gets batch processing statistics.
    /// </summary>
    /// <param name="fromTime">The start time for batch analysis (null for all time).</param>
    /// <param name="toTime">The end time for batch analysis (null for current time).</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains batch processing statistics and completion rates.
    /// </returns>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method provides comprehensive batch processing statistics including
    /// completion rates, timeout frequencies, and batch size distributions for
    /// batch processing optimization and monitoring.
    /// </remarks>
    Task<BatchMetrics> GetBatchMetricsAsync(DateTime? fromTime = null, DateTime? toTime = null);

    /// <summary>
    /// Resets all collected metrics. Use with caution.
    /// </summary>
    /// <exception cref="System.ObjectDisposedException">
    /// Thrown when the metrics service has been disposed.
    /// </exception>
    /// <remarks>
    /// This method clears all collected metrics data and resets counters to zero.
    /// This operation is irreversible and should only be used in testing scenarios
    /// or when explicitly required for metrics management.
    /// 
    /// Warning: This will permanently delete all historical metrics data.
    /// </remarks>
    void ResetMetrics();
}
