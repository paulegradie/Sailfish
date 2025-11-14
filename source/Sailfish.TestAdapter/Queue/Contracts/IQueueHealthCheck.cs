using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for a service that monitors the health and performance of the queue system.
/// This interface is part of the intercepting queue architecture that enables comprehensive monitoring
/// and diagnostics of queue operations, processing rates, and system performance.
/// </summary>
/// <remarks>
/// The IQueueHealthCheck provides real-time monitoring capabilities for the queue infrastructure,
/// including queue depth monitoring, processing rate tracking, error rate analysis, and system
/// degradation detection. This enables proactive identification of performance issues and system
/// failures before they impact test execution.
/// 
/// Key responsibilities:
/// - Monitor queue operational status and performance metrics
/// - Track processing rates and identify performance bottlenecks
/// - Detect error patterns and system degradation scenarios
/// - Provide health status reporting for diagnostics and alerting
/// - Support configurable monitoring intervals and alert thresholds
/// - Enable proactive monitoring and early warning systems
/// 
/// The health check service operates as a background monitoring service that periodically
/// evaluates queue system health and provides both on-demand status queries and event-driven
/// notifications when health status changes occur.
/// 
/// Thread Safety:
/// All methods in this interface must be thread-safe and support concurrent access from
/// multiple threads. Implementations should use appropriate synchronization mechanisms
/// to ensure data consistency and prevent race conditions.
/// 
/// Integration:
/// This service integrates with the existing queue infrastructure components including
/// TestCompletionQueueManager, queue consumers, and batch processing services to provide
/// comprehensive system monitoring capabilities.
/// </remarks>
public interface IQueueHealthCheck
{
    /// <summary>
    /// Starts the health monitoring service and begins periodic health checks.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the start operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous start operation.
    /// The task completes when health monitoring has been started successfully.
    /// </returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the health check service is already running.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method should be called during queue system startup to begin monitoring
    /// queue health and performance. The service will start periodic health checks
    /// and begin tracking system metrics for analysis and reporting.
    /// </remarks>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the health monitoring service gracefully and ceases all monitoring activities.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the stop operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous stop operation.
    /// The task completes when health monitoring has been stopped gracefully.
    /// </returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method should be called during queue system shutdown to ensure proper
    /// cleanup of monitoring resources and graceful termination of health check activities.
    /// </remarks>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current health status of the queue system.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the current health status of the queue system.
    /// </returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides an on-demand health status check that evaluates current
    /// queue metrics and system performance to determine the overall health status.
    /// The status includes both the health level and descriptive information about
    /// the current system state.
    /// </remarks>
    Task<QueueHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets comprehensive health metrics for the queue system.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains detailed health metrics for analysis and monitoring.
    /// </returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides detailed metrics about queue performance, processing rates,
    /// error rates, and other system performance indicators. These metrics can be used
    /// for performance analysis, capacity planning, and troubleshooting.
    /// </remarks>
    Task<QueueHealthMetrics> GetHealthMetricsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Occurs when the health status of the queue system changes.
    /// </summary>
    /// <remarks>
    /// This event is raised whenever the health monitoring service detects a change
    /// in the overall health status of the queue system. Subscribers can use this
    /// event to implement alerting, logging, or other reactive monitoring behaviors.
    /// 
    /// The event provides both the previous and current health status to enable
    /// appropriate responses to health status transitions.
    /// </remarks>
    event EventHandler<QueueHealthStatusChangedEventArgs>? HealthStatusChanged;
}

/// <summary>
/// Represents the health status levels for the queue system.
/// </summary>
/// <remarks>
/// These status levels provide a standardized way to categorize the overall health
/// of the queue system based on various performance metrics and operational indicators.
/// </remarks>
public enum QueueHealthLevel
{
    /// <summary>
    /// The queue system is operating normally with all metrics within acceptable ranges.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// The queue system is operational but some metrics are approaching warning thresholds.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// The queue system is experiencing issues that may impact performance or reliability.
    /// </summary>
    Unhealthy = 2,

    /// <summary>
    /// The queue system is experiencing critical failures or severe performance degradation.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Represents comprehensive health status information for the queue system.
/// </summary>
/// <remarks>
/// This record provides a complete snapshot of the queue system's health status,
/// including both the overall health level and detailed information about specific
/// metrics and indicators that contributed to the health assessment.
/// </remarks>
public record QueueHealthStatus
{
    /// <summary>
    /// Represents comprehensive health status information for the queue system.
    /// </summary>
    /// <param name="Level">The overall health level of the queue system.</param>
    /// <param name="Status">A human-readable description of the current health status.</param>
    /// <param name="Timestamp">The timestamp when this health status was determined.</param>
    /// <param name="Details">Additional details about specific health indicators and metrics.</param>
    /// <remarks>
    /// This record provides a complete snapshot of the queue system's health status,
    /// including both the overall health level and detailed information about specific
    /// metrics and indicators that contributed to the health assessment.
    /// </remarks>
    public QueueHealthStatus(QueueHealthLevel Level,
        string Status,
        DateTime Timestamp,
        Dictionary<string, object> Details)
    {
        this.Level = Level;
        this.Status = Status;
        this.Timestamp = Timestamp;
        this.Details = Details;
    }

    /// <summary>The overall health level of the queue system.</summary>
    public QueueHealthLevel Level { get; init; }

    /// <summary>A human-readable description of the current health status.</summary>
    public string Status { get; init; }

    /// <summary>The timestamp when this health status was determined.</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>Additional details about specific health indicators and metrics.</summary>
    public Dictionary<string, object> Details { get; init; }

    public void Deconstruct(out QueueHealthLevel Level, out string Status, out DateTime Timestamp, out Dictionary<string, object> Details)
    {
        Level = this.Level;
        Status = this.Status;
        Timestamp = this.Timestamp;
        Details = this.Details;
    }
}

/// <summary>
/// Represents comprehensive performance and health metrics for the queue system.
/// </summary>
/// <remarks>
/// This record provides detailed performance metrics that can be used for monitoring,
/// analysis, and troubleshooting of the queue system. The metrics cover queue performance,
/// processing efficiency, error rates, and batch processing statistics.
/// </remarks>
public record QueueHealthMetrics
{
    /// <summary>
    /// Represents comprehensive performance and health metrics for the queue system.
    /// </summary>
    /// <param name="QueueDepth">Current number of messages in the queue.</param>
    /// <param name="AverageQueueDepth">Average queue depth over the monitoring period.</param>
    /// <param name="PeakQueueDepth">Maximum queue depth observed during the monitoring period.</param>
    /// <param name="MessagesProcessedPerSecond">Current message processing rate.</param>
    /// <param name="AverageProcessingTimeMs">Average time to process a message in milliseconds.</param>
    /// <param name="ErrorRate">Percentage of messages that failed processing.</param>
    /// <param name="RetryRate">Percentage of messages that required retry attempts.</param>
    /// <param name="BatchCompletionRate">Percentage of batches that completed successfully.</param>
    /// <param name="BatchTimeoutRate">Percentage of batches that timed out.</param>
    /// <param name="SystemUptime">Total uptime of the queue system.</param>
    /// <param name="LastHealthCheck">Timestamp of the last health check.</param>
    /// <param name="AdditionalMetrics">Additional implementation-specific metrics.</param>
    /// <remarks>
    /// This record provides detailed performance metrics that can be used for monitoring,
    /// analysis, and troubleshooting of the queue system. The metrics cover queue performance,
    /// processing efficiency, error rates, and batch processing statistics.
    /// </remarks>
    public QueueHealthMetrics(int QueueDepth,
        double AverageQueueDepth,
        int PeakQueueDepth,
        double MessagesProcessedPerSecond,
        double AverageProcessingTimeMs,
        double ErrorRate,
        double RetryRate,
        double BatchCompletionRate,
        double BatchTimeoutRate,
        TimeSpan SystemUptime,
        DateTime LastHealthCheck,
        Dictionary<string, object> AdditionalMetrics)
    {
        this.QueueDepth = QueueDepth;
        this.AverageQueueDepth = AverageQueueDepth;
        this.PeakQueueDepth = PeakQueueDepth;
        this.MessagesProcessedPerSecond = MessagesProcessedPerSecond;
        this.AverageProcessingTimeMs = AverageProcessingTimeMs;
        this.ErrorRate = ErrorRate;
        this.RetryRate = RetryRate;
        this.BatchCompletionRate = BatchCompletionRate;
        this.BatchTimeoutRate = BatchTimeoutRate;
        this.SystemUptime = SystemUptime;
        this.LastHealthCheck = LastHealthCheck;
        this.AdditionalMetrics = AdditionalMetrics;
    }

    /// <summary>Current number of messages in the queue.</summary>
    public int QueueDepth { get; init; }

    /// <summary>Average queue depth over the monitoring period.</summary>
    public double AverageQueueDepth { get; init; }

    /// <summary>Maximum queue depth observed during the monitoring period.</summary>
    public int PeakQueueDepth { get; init; }

    /// <summary>Current message processing rate.</summary>
    public double MessagesProcessedPerSecond { get; init; }

    /// <summary>Average time to process a message in milliseconds.</summary>
    public double AverageProcessingTimeMs { get; init; }

    /// <summary>Percentage of messages that failed processing.</summary>
    public double ErrorRate { get; init; }

    /// <summary>Percentage of messages that required retry attempts.</summary>
    public double RetryRate { get; init; }

    /// <summary>Percentage of batches that completed successfully.</summary>
    public double BatchCompletionRate { get; init; }

    /// <summary>Percentage of batches that timed out.</summary>
    public double BatchTimeoutRate { get; init; }

    /// <summary>Total uptime of the queue system.</summary>
    public TimeSpan SystemUptime { get; init; }

    /// <summary>Timestamp of the last health check.</summary>
    public DateTime LastHealthCheck { get; init; }

    /// <summary>Additional implementation-specific metrics.</summary>
    public Dictionary<string, object> AdditionalMetrics { get; init; }

    public void Deconstruct(out int QueueDepth, out double AverageQueueDepth, out int PeakQueueDepth, out double MessagesProcessedPerSecond, out double AverageProcessingTimeMs, out double ErrorRate, out double RetryRate, out double BatchCompletionRate, out double BatchTimeoutRate, out TimeSpan SystemUptime, out DateTime LastHealthCheck, out Dictionary<string, object> AdditionalMetrics)
    {
        QueueDepth = this.QueueDepth;
        AverageQueueDepth = this.AverageQueueDepth;
        PeakQueueDepth = this.PeakQueueDepth;
        MessagesProcessedPerSecond = this.MessagesProcessedPerSecond;
        AverageProcessingTimeMs = this.AverageProcessingTimeMs;
        ErrorRate = this.ErrorRate;
        RetryRate = this.RetryRate;
        BatchCompletionRate = this.BatchCompletionRate;
        BatchTimeoutRate = this.BatchTimeoutRate;
        SystemUptime = this.SystemUptime;
        LastHealthCheck = this.LastHealthCheck;
        AdditionalMetrics = this.AdditionalMetrics;
    }
}

/// <summary>
/// Provides data for the HealthStatusChanged event.
/// </summary>
/// <remarks>
/// This event args class provides information about health status transitions,
/// enabling subscribers to implement appropriate responses to health status changes.
/// </remarks>
public record QueueHealthStatusChangedEventArgs
{
    /// <summary>
    /// Provides data for the HealthStatusChanged event.
    /// </summary>
    /// <param name="PreviousStatus">The previous health status before the change.</param>
    /// <param name="CurrentStatus">The current health status after the change.</param>
    /// <param name="ChangeTimestamp">The timestamp when the health status change was detected.</param>
    /// <remarks>
    /// This event args class provides information about health status transitions,
    /// enabling subscribers to implement appropriate responses to health status changes.
    /// </remarks>
    public QueueHealthStatusChangedEventArgs(QueueHealthStatus PreviousStatus,
        QueueHealthStatus CurrentStatus,
        DateTime ChangeTimestamp)
    {
        this.PreviousStatus = PreviousStatus;
        this.CurrentStatus = CurrentStatus;
        this.ChangeTimestamp = ChangeTimestamp;
    }

    /// <summary>The previous health status before the change.</summary>
    public QueueHealthStatus PreviousStatus { get; init; }

    /// <summary>The current health status after the change.</summary>
    public QueueHealthStatus CurrentStatus { get; init; }

    /// <summary>The timestamp when the health status change was detected.</summary>
    public DateTime ChangeTimestamp { get; init; }

    public void Deconstruct(out QueueHealthStatus PreviousStatus, out QueueHealthStatus CurrentStatus, out DateTime ChangeTimestamp)
    {
        PreviousStatus = this.PreviousStatus;
        CurrentStatus = this.CurrentStatus;
        ChangeTimestamp = this.ChangeTimestamp;
    }
}
