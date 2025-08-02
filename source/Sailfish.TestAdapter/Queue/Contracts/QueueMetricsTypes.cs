using System;
using System.Collections.Generic;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Represents a comprehensive snapshot of queue metrics for a specific time period.
/// </summary>
/// <param name="StartTime">The start time of the metrics collection period.</param>
/// <param name="EndTime">The end time of the metrics collection period.</param>
/// <param name="MessagesPublished">Total number of messages published to the queue.</param>
/// <param name="MessagesProcessed">Total number of messages successfully processed.</param>
/// <param name="MessagesFailed">Total number of messages that failed processing.</param>
/// <param name="AverageProcessingTimeMs">Average time to process a message in milliseconds.</param>
/// <param name="ProcessingRatePerSecond">Average number of messages processed per second.</param>
/// <param name="ErrorRate">Percentage of messages that failed processing.</param>
/// <param name="QueueDepthStats">Statistics about queue depth over the time period.</param>
/// <param name="BatchStats">Statistics about batch processing over the time period.</param>
/// <param name="ProcessorStats">Per-processor statistics and performance metrics.</param>
/// <remarks>
/// This record provides a comprehensive snapshot of all queue metrics for a specific
/// time period, enabling detailed analysis of queue performance, processing efficiency,
/// and system health indicators.
/// </remarks>
public record QueueMetricsSnapshot(
    DateTime StartTime,
    DateTime EndTime,
    long MessagesPublished,
    long MessagesProcessed,
    long MessagesFailed,
    double AverageProcessingTimeMs,
    double ProcessingRatePerSecond,
    double ErrorRate,
    QueueDepthStats QueueDepthStats,
    BatchStats BatchStats,
    Dictionary<string, ProcessorStats> ProcessorStats
);

/// <summary>
/// Represents processing rate metrics and performance indicators.
/// </summary>
/// <param name="CurrentRatePerSecond">Current processing rate in messages per second.</param>
/// <param name="AverageRatePerSecond">Average processing rate over the monitoring period.</param>
/// <param name="PeakRatePerSecond">Peak processing rate observed.</param>
/// <param name="CurrentLatencyMs">Current average processing latency in milliseconds.</param>
/// <param name="AverageLatencyMs">Average processing latency over the monitoring period.</param>
/// <param name="PeakLatencyMs">Peak processing latency observed.</param>
/// <param name="ThroughputTrend">Trend indicator for throughput (increasing, decreasing, stable).</param>
/// <param name="LatencyTrend">Trend indicator for latency (increasing, decreasing, stable).</param>
/// <remarks>
/// This record provides real-time processing rate information including current
/// throughput, latency measurements, and trend indicators for performance monitoring.
/// </remarks>
public record ProcessingRateMetrics(
    double CurrentRatePerSecond,
    double AverageRatePerSecond,
    double PeakRatePerSecond,
    double CurrentLatencyMs,
    double AverageLatencyMs,
    double PeakLatencyMs,
    string ThroughputTrend,
    string LatencyTrend
);

/// <summary>
/// Represents queue depth metrics and statistics over time.
/// </summary>
/// <param name="CurrentDepth">Current number of messages in the queue.</param>
/// <param name="AverageDepth">Average queue depth over the monitoring period.</param>
/// <param name="PeakDepth">Maximum queue depth observed.</param>
/// <param name="MinimumDepth">Minimum queue depth observed.</param>
/// <param name="DepthTrend">Trend indicator for queue depth (increasing, decreasing, stable).</param>
/// <param name="DepthHistory">Historical queue depth measurements.</param>
/// <remarks>
/// This record provides detailed analysis of queue depth changes over time,
/// including average depth, peak usage, and trend analysis for capacity planning.
/// </remarks>
public record QueueDepthMetrics(
    int CurrentDepth,
    double AverageDepth,
    int PeakDepth,
    int MinimumDepth,
    string DepthTrend,
    List<QueueDepthMeasurement> DepthHistory
);

/// <summary>
/// Represents batch processing metrics and completion statistics.
/// </summary>
/// <param name="TotalBatches">Total number of batches processed.</param>
/// <param name="CompletedBatches">Number of batches that completed successfully.</param>
/// <param name="TimedOutBatches">Number of batches that timed out.</param>
/// <param name="CompletionRate">Percentage of batches that completed successfully.</param>
/// <param name="TimeoutRate">Percentage of batches that timed out.</param>
/// <param name="AverageBatchSize">Average number of test cases per batch.</param>
/// <param name="AverageBatchProcessingTimeMs">Average time to process a batch in milliseconds.</param>
/// <param name="BatchSizeDistribution">Distribution of batch sizes.</param>
/// <remarks>
/// This record provides comprehensive batch processing statistics including
/// completion rates, timeout frequencies, and batch size distributions for
/// batch processing optimization and monitoring.
/// </remarks>
public record BatchMetrics(
    long TotalBatches,
    long CompletedBatches,
    long TimedOutBatches,
    double CompletionRate,
    double TimeoutRate,
    double AverageBatchSize,
    double AverageBatchProcessingTimeMs,
    Dictionary<int, int> BatchSizeDistribution
);

/// <summary>
/// Represents queue depth statistics for a specific time period.
/// </summary>
/// <param name="Average">Average queue depth over the time period.</param>
/// <param name="Peak">Maximum queue depth observed during the time period.</param>
/// <param name="Minimum">Minimum queue depth observed during the time period.</param>
/// <param name="Trend">Trend indicator for queue depth (increasing, decreasing, stable).</param>
/// <remarks>
/// This record provides summary statistics about queue depth behavior over a time period.
/// </remarks>
public record QueueDepthStats(double Average, int Peak, int Minimum, string Trend);

/// <summary>
/// Represents batch processing statistics for a specific time period.
/// </summary>
/// <param name="Total">Total number of batches processed.</param>
/// <param name="Completed">Number of batches that completed successfully.</param>
/// <param name="TimedOut">Number of batches that timed out.</param>
/// <param name="CompletionRate">Percentage of batches that completed successfully.</param>
/// <param name="TimeoutRate">Percentage of batches that timed out.</param>
/// <remarks>
/// This record provides summary statistics about batch processing behavior over a time period.
/// </remarks>
public record BatchStats(long Total, long Completed, long TimedOut, double CompletionRate, double TimeoutRate);

/// <summary>
/// Represents processor performance statistics for a specific time period.
/// </summary>
/// <param name="Processed">Number of messages successfully processed.</param>
/// <param name="Failed">Number of messages that failed processing.</param>
/// <param name="AverageTimeMs">Average processing time in milliseconds.</param>
/// <param name="ErrorRate">Percentage of messages that failed processing.</param>
/// <remarks>
/// This record provides performance statistics for individual queue processors.
/// </remarks>
public record ProcessorStats(long Processed, long Failed, double AverageTimeMs, double ErrorRate);

/// <summary>
/// Represents a single queue depth measurement at a specific point in time.
/// </summary>
/// <param name="Timestamp">The timestamp when the measurement was taken.</param>
/// <param name="Depth">The number of messages in the queue at the time of measurement.</param>
/// <remarks>
/// This record is used to track queue depth changes over time for trend analysis.
/// </remarks>
public record QueueDepthMeasurement(DateTime Timestamp, int Depth);
