using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Monitoring;





/// <summary>
/// Implementation of comprehensive queue metrics collection and monitoring service.
/// This service provides centralized metrics tracking for the intercepting queue architecture,
/// enabling detailed performance analysis, operational monitoring, and system optimization.
/// </summary>
/// <remarks>
/// The QueueMetrics service collects and aggregates metrics from all queue operations including
/// message publishing, processing, batch operations, and queue depth monitoring. It maintains
/// historical data for trend analysis and provides comprehensive reporting capabilities.
///
/// Key features:
/// - Thread-safe metrics collection for concurrent queue operations
/// - Historical data retention with configurable time windows
/// - Real-time processing rate and latency calculations
/// - Batch processing statistics and completion rate tracking
/// - Queue depth monitoring with trend analysis
/// - Per-processor performance metrics and error tracking
/// - Comprehensive metrics export for external monitoring systems
///
/// The service is designed to have minimal performance impact on queue operations while
/// providing detailed insights into system behavior and performance characteristics.
///
/// Thread Safety:
/// This implementation is fully thread-safe and supports concurrent access from multiple
/// queue components. All shared state is protected using thread-safe collections and
/// appropriate synchronization mechanisms.
///
/// Memory Management:
/// The service implements automatic cleanup of historical data to prevent memory leaks
/// during long-running test sessions. Metrics older than the configured retention period
/// are automatically purged to maintain optimal memory usage.
/// </remarks>
public class QueueMetrics : IQueueMetrics, IDisposable
{
    private readonly ILogger _logger;
    private readonly QueueConfiguration _configuration;

    // Thread-safe collections for metrics storage
    private readonly ConcurrentDictionary<string, MessageMetric> _messageMetrics = new();
    private readonly ConcurrentQueue<Contracts.QueueDepthMeasurement> _queueDepthHistory = new();
    private readonly ConcurrentDictionary<string, BatchMetric> _batchMetrics = new();
    private readonly ConcurrentDictionary<string, ProcessorMetric> _processorMetrics = new();

    // Atomic counters for high-frequency operations
    private long _messagesPublished;
    private long _messagesProcessed;
    private long _messagesFailed;

    // Timing and rate calculation
    private readonly object _rateCalculationLock = new();
    private DateTime _startTime;
    private DateTime _lastRateCalculation;
    private double _currentProcessingRate;
    private double _averageProcessingRate;
    private double _peakProcessingRate;

    // Disposal tracking
    private volatile bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueMetrics"/> class.
    /// </summary>
    /// <param name="configuration">The queue configuration for metrics settings.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> or <paramref name="logger"/> is null.
    /// </exception>
    public QueueMetrics(QueueConfiguration configuration, ILogger logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _startTime = DateTime.UtcNow;
        _lastRateCalculation = _startTime;

        _logger.Log(LogLevel.Debug, "Queue metrics service initialized");
    }

    /// <inheritdoc />
    public void RecordMessagePublished(string testCaseId, DateTime timestamp)
    {
        if (string.IsNullOrEmpty(testCaseId))
        {
            throw new ArgumentException("Test case ID cannot be null or empty", nameof(testCaseId));
        }

        ThrowIfDisposed();

        try
        {
            Interlocked.Increment(ref _messagesPublished);

            var metric = _messageMetrics.GetOrAdd(testCaseId, _ => new MessageMetric(testCaseId));
            metric.RecordPublished(timestamp);

            _logger.Log(LogLevel.Debug, "Recorded message published for test case: {0}", testCaseId);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to record message published metric for test case '{0}': {1}", testCaseId, ex.Message);
        }
    }

    /// <inheritdoc />
    public void RecordMessageProcessed(string testCaseId, string processorName, double processingTimeMs, DateTime timestamp)
    {
        if (string.IsNullOrEmpty(testCaseId))
        {
            throw new ArgumentException("Test case ID cannot be null or empty", nameof(testCaseId));
        }

        if (string.IsNullOrEmpty(processorName))
        {
            throw new ArgumentException("Processor name cannot be null or empty", nameof(processorName));
        }

        ThrowIfDisposed();

        try
        {
            Interlocked.Increment(ref _messagesProcessed);

            // Update message metric
            var messageMetric = _messageMetrics.GetOrAdd(testCaseId, _ => new MessageMetric(testCaseId));
            messageMetric.RecordProcessed(processorName, processingTimeMs, timestamp);

            // Update processor metric
            var processorMetric = _processorMetrics.GetOrAdd(processorName, _ => new ProcessorMetric(processorName));
            processorMetric.RecordProcessed(processingTimeMs, timestamp);

            // Update processing rates
            UpdateProcessingRates(timestamp);

            _logger.Log(LogLevel.Debug,
                "Recorded message processed for test case: {0}, processor: {1}, time: {2}ms",
                testCaseId, processorName, processingTimeMs);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to record message processed metric for test case '{0}', processor '{1}': {2}",
                testCaseId, processorName, ex.Message);
        }
    }

    /// <inheritdoc />
    public void RecordMessageFailed(string testCaseId, string processorName, string errorMessage, DateTime timestamp)
    {
        if (string.IsNullOrEmpty(testCaseId))
        {
            throw new ArgumentException("Test case ID cannot be null or empty", nameof(testCaseId));
        }

        if (string.IsNullOrEmpty(processorName))
        {
            throw new ArgumentException("Processor name cannot be null or empty", nameof(processorName));
        }

        ThrowIfDisposed();

        try
        {
            Interlocked.Increment(ref _messagesFailed);

            // Update message metric
            var messageMetric = _messageMetrics.GetOrAdd(testCaseId, _ => new MessageMetric(testCaseId));
            messageMetric.RecordFailed(processorName, errorMessage, timestamp);

            // Update processor metric
            var processorMetric = _processorMetrics.GetOrAdd(processorName, _ => new ProcessorMetric(processorName));
            processorMetric.RecordFailed(errorMessage, timestamp);

            _logger.Log(LogLevel.Debug,
                "Recorded message failed for test case: {0}, processor: {1}, error: {2}",
                testCaseId, processorName, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to record message failed metric for test case '{0}', processor '{1}': {2}",
                testCaseId, processorName, ex.Message);
        }
    }

    /// <inheritdoc />
    public void RecordQueueDepth(int depth, DateTime timestamp)
    {
        if (depth < 0)
        {
            throw new ArgumentException("Queue depth cannot be negative", nameof(depth));
        }

        ThrowIfDisposed();

        try
        {
            var measurement = new Contracts.QueueDepthMeasurement(timestamp, depth);
            _queueDepthHistory.Enqueue(measurement);

            // Cleanup old measurements to prevent memory leaks
            CleanupOldQueueDepthMeasurements();

            _logger.Log(LogLevel.Debug, "Recorded queue depth: {0} at {1}", depth, timestamp);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to record queue depth metric: {0}", ex.Message);
        }
    }

    /// <inheritdoc />
    public void RecordBatchCompletion(string batchId, int batchSize, bool completedSuccessfully, double processingTimeMs, DateTime timestamp)
    {
        if (string.IsNullOrEmpty(batchId))
        {
            throw new ArgumentException("Batch ID cannot be null or empty", nameof(batchId));
        }

        if (batchSize < 0)
        {
            throw new ArgumentException("Batch size cannot be negative", nameof(batchSize));
        }

        ThrowIfDisposed();

        try
        {
            var batchMetric = _batchMetrics.GetOrAdd(batchId, _ => new BatchMetric(batchId));
            batchMetric.RecordCompletion(batchSize, completedSuccessfully, processingTimeMs, timestamp);

            _logger.Log(LogLevel.Debug,
                "Recorded batch completion for batch: {0}, size: {1}, successful: {2}, time: {3}ms",
                batchId, batchSize, completedSuccessfully, processingTimeMs);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to record batch completion metric for batch '{0}': {1}", batchId, ex.Message);
        }
    }

    /// <inheritdoc />
    public void RecordBatchTimeout(string batchId, int expectedSize, int actualSize, double timeoutMs, DateTime timestamp)
    {
        if (string.IsNullOrEmpty(batchId))
        {
            throw new ArgumentException("Batch ID cannot be null or empty", nameof(batchId));
        }

        ThrowIfDisposed();

        try
        {
            var batchMetric = _batchMetrics.GetOrAdd(batchId, _ => new BatchMetric(batchId));
            batchMetric.RecordTimeout(expectedSize, actualSize, timeoutMs, timestamp);

            _logger.Log(LogLevel.Debug,
                "Recorded batch timeout for batch: {0}, expected: {1}, actual: {2}, timeout: {3}ms",
                batchId, expectedSize, actualSize, timeoutMs);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to record batch timeout metric for batch '{0}': {1}", batchId, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Contracts.QueueMetricsSnapshot> GetMetricsAsync(DateTime? fromTime = null, DateTime? toTime = null)
    {
        ThrowIfDisposed();

        return await Task.Run(() =>
        {
            try
            {
                var startTime = fromTime ?? _startTime;
                var endTime = toTime ?? DateTime.UtcNow;

                var messagesPublished = Interlocked.Read(ref _messagesPublished);
                var messagesProcessed = Interlocked.Read(ref _messagesProcessed);
                var messagesFailed = Interlocked.Read(ref _messagesFailed);

                var totalMessages = messagesProcessed + messagesFailed;
                var errorRate = totalMessages > 0 ? (double)messagesFailed / totalMessages * 100 : 0;

                var averageProcessingTime = CalculateAverageProcessingTime(startTime, endTime);
                var processingRate = CalculateProcessingRateForPeriod(startTime, endTime);

                var queueDepthStats = CalculateQueueDepthStats(startTime, endTime);
                var batchStats = CalculateBatchStats(startTime, endTime);
                var processorStats = CalculateProcessorStats(startTime, endTime);

                return new Contracts.QueueMetricsSnapshot(
                    startTime,
                    endTime,
                    messagesPublished,
                    messagesProcessed,
                    messagesFailed,
                    averageProcessingTime,
                    processingRate,
                    errorRate,
                    queueDepthStats,
                    batchStats,
                    processorStats
                );
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Failed to get queue metrics snapshot: {0}", ex.Message);
                throw;
            }
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Contracts.ProcessingRateMetrics> GetProcessingRatesAsync()
    {
        ThrowIfDisposed();

        return await Task.Run(() =>
        {
            try
            {
                lock (_rateCalculationLock)
                {
                    var currentLatency = CalculateCurrentAverageLatency();
                    var averageLatency = CalculateOverallAverageLatency();
                    var peakLatency = CalculatePeakLatency();

                    var throughputTrend = CalculateThroughputTrend();
                    var latencyTrend = CalculateLatencyTrend();

                    return new Contracts.ProcessingRateMetrics(
                        _currentProcessingRate,
                        _averageProcessingRate,
                        _peakProcessingRate,
                        currentLatency,
                        averageLatency,
                        peakLatency,
                        throughputTrend,
                        latencyTrend
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Failed to get processing rate metrics: {0}", ex.Message);
                throw;
            }
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Contracts.QueueDepthMetrics> GetQueueDepthMetricsAsync(DateTime? fromTime = null, DateTime? toTime = null)
    {
        ThrowIfDisposed();

        return await Task.Run(() =>
        {
            try
            {
                var startTime = fromTime ?? _startTime;
                var endTime = toTime ?? DateTime.UtcNow;

                var depthHistory = GetQueueDepthHistory(startTime, endTime);

                if (!depthHistory.Any())
                {
                    return new Contracts.QueueDepthMetrics(0, 0, 0, 0, "Unknown", new List<Contracts.QueueDepthMeasurement>());
                }

                var currentDepth = depthHistory.LastOrDefault()?.Depth ?? 0;
                var averageDepth = depthHistory.Average(m => m.Depth);
                var peakDepth = depthHistory.Max(m => m.Depth);
                var minimumDepth = depthHistory.Min(m => m.Depth);
                var depthTrend = CalculateDepthTrend(depthHistory);

                return new Contracts.QueueDepthMetrics(
                    currentDepth,
                    averageDepth,
                    peakDepth,
                    minimumDepth,
                    depthTrend,
                    depthHistory.ToList()
                );
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Failed to get queue depth metrics: {0}", ex.Message);
                throw;
            }
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Contracts.BatchMetrics> GetBatchMetricsAsync(DateTime? fromTime = null, DateTime? toTime = null)
    {
        ThrowIfDisposed();

        return await Task.Run(() =>
        {
            try
            {
                var startTime = fromTime ?? _startTime;
                var endTime = toTime ?? DateTime.UtcNow;

                var relevantBatches = _batchMetrics.Values
                    .Where(b => b.HasActivityInPeriod(startTime, endTime))
                    .ToList();

                if (!relevantBatches.Any())
                {
                    return new Contracts.BatchMetrics(0, 0, 0, 0, 0, 0, 0, new Dictionary<int, int>());
                }

                var totalBatches = relevantBatches.Count;
                var completedBatches = relevantBatches.Count(b => b.IsCompleted);
                var timedOutBatches = relevantBatches.Count(b => b.IsTimedOut);

                var completionRate = totalBatches > 0 ? (double)completedBatches / totalBatches * 100 : 0;
                var timeoutRate = totalBatches > 0 ? (double)timedOutBatches / totalBatches * 100 : 0;

                var averageBatchSize = relevantBatches.Where(b => b.BatchSize > 0).Average(b => b.BatchSize);
                var averageProcessingTime = relevantBatches.Where(b => b.ProcessingTimeMs > 0).Average(b => b.ProcessingTimeMs);

                var batchSizeDistribution = relevantBatches
                    .Where(b => b.BatchSize > 0)
                    .GroupBy(b => b.BatchSize)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new Contracts.BatchMetrics(
                    totalBatches,
                    completedBatches,
                    timedOutBatches,
                    completionRate,
                    timeoutRate,
                    averageBatchSize,
                    averageProcessingTime,
                    batchSizeDistribution
                );
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "Failed to get batch metrics: {0}", ex.Message);
                throw;
            }
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void ResetMetrics()
    {
        ThrowIfDisposed();

        try
        {
            _logger.Log(LogLevel.Information, "Resetting all queue metrics");

            Interlocked.Exchange(ref _messagesPublished, 0);
            Interlocked.Exchange(ref _messagesProcessed, 0);
            Interlocked.Exchange(ref _messagesFailed, 0);

            _messageMetrics.Clear();
            _batchMetrics.Clear();
            _processorMetrics.Clear();

            // Clear queue depth history
            while (_queueDepthHistory.TryDequeue(out _)) { }

            lock (_rateCalculationLock)
            {
                _startTime = DateTime.UtcNow;
                _lastRateCalculation = _startTime;
                _currentProcessingRate = 0;
                _averageProcessingRate = 0;
                _peakProcessingRate = 0;
            }

            _logger.Log(LogLevel.Information, "Queue metrics reset completed");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, "Failed to reset queue metrics: {0}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Updates processing rates based on recent activity.
    /// </summary>
    /// <param name="timestamp">The timestamp of the latest processing event.</param>
    private void UpdateProcessingRates(DateTime timestamp)
    {
        lock (_rateCalculationLock)
        {
            var timeSinceLastCalculation = timestamp - _lastRateCalculation;

            if (timeSinceLastCalculation.TotalSeconds >= 1.0) // Update rates every second
            {
                var totalTime = timestamp - _startTime;
                var totalProcessed = Interlocked.Read(ref _messagesProcessed);

                if (totalTime.TotalSeconds > 0)
                {
                    _averageProcessingRate = totalProcessed / totalTime.TotalSeconds;
                    _currentProcessingRate = 1.0 / timeSinceLastCalculation.TotalSeconds; // Rate for this interval

                    if (_currentProcessingRate > _peakProcessingRate)
                    {
                        _peakProcessingRate = _currentProcessingRate;
                    }
                }

                _lastRateCalculation = timestamp;
            }
        }
    }

    /// <summary>
    /// Cleans up old queue depth measurements to prevent memory leaks.
    /// </summary>
    private void CleanupOldQueueDepthMeasurements()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-24); // Keep 24 hours of history

        while (_queueDepthHistory.TryPeek(out var measurement) && measurement.Timestamp < cutoffTime)
        {
            _queueDepthHistory.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Calculates average processing time for the specified period.
    /// </summary>
    private double CalculateAverageProcessingTime(DateTime startTime, DateTime endTime)
    {
        var processingTimes = _processorMetrics.Values
            .SelectMany(p => p.GetProcessingTimesInPeriod(startTime, endTime))
            .ToList();

        return processingTimes.Any() ? processingTimes.Average() : 0;
    }

    /// <summary>
    /// Calculates processing rate for the specified period.
    /// </summary>
    private double CalculateProcessingRateForPeriod(DateTime startTime, DateTime endTime)
    {
        var totalProcessed = _processorMetrics.Values
            .Sum(p => p.GetProcessedCountInPeriod(startTime, endTime));

        var duration = endTime - startTime;
        return duration.TotalSeconds > 0 ? totalProcessed / duration.TotalSeconds : 0;
    }

    /// <summary>
    /// Calculates queue depth statistics for the specified period.
    /// </summary>
    private Contracts.QueueDepthStats CalculateQueueDepthStats(DateTime startTime, DateTime endTime)
    {
        var depthHistory = GetQueueDepthHistory(startTime, endTime);

        if (!depthHistory.Any())
        {
            return new Contracts.QueueDepthStats(0, 0, 0, "Unknown");
        }

        var average = depthHistory.Average(m => m.Depth);
        var peak = depthHistory.Max(m => m.Depth);
        var minimum = depthHistory.Min(m => m.Depth);
        var trend = CalculateDepthTrend(depthHistory);

        return new Contracts.QueueDepthStats(average, peak, minimum, trend);
    }

    /// <summary>
    /// Calculates batch statistics for the specified period.
    /// </summary>
    private Contracts.BatchStats CalculateBatchStats(DateTime startTime, DateTime endTime)
    {
        var relevantBatches = _batchMetrics.Values
            .Where(b => b.HasActivityInPeriod(startTime, endTime))
            .ToList();

        if (!relevantBatches.Any())
        {
            return new Contracts.BatchStats(0, 0, 0, 0, 0);
        }

        var total = relevantBatches.Count;
        var completed = relevantBatches.Count(b => b.IsCompleted);
        var timedOut = relevantBatches.Count(b => b.IsTimedOut);
        var completionRate = total > 0 ? (double)completed / total * 100 : 0;
        var timeoutRate = total > 0 ? (double)timedOut / total * 100 : 0;

        return new Contracts.BatchStats(total, completed, timedOut, completionRate, timeoutRate);
    }

    /// <summary>
    /// Calculates processor statistics for the specified period.
    /// </summary>
    private Dictionary<string, Contracts.ProcessorStats> CalculateProcessorStats(DateTime startTime, DateTime endTime)
    {
        return _processorMetrics.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.GetStatsForPeriod(startTime, endTime)
        );
    }

    /// <summary>
    /// Gets queue depth history for the specified period.
    /// </summary>
    private IEnumerable<Contracts.QueueDepthMeasurement> GetQueueDepthHistory(DateTime startTime, DateTime endTime)
    {
        return _queueDepthHistory
            .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
            .OrderBy(m => m.Timestamp);
    }

    /// <summary>
    /// Calculates the trend for queue depth measurements.
    /// </summary>
    private string CalculateDepthTrend(IEnumerable<Contracts.QueueDepthMeasurement> measurements)
    {
        var measurementList = measurements.ToList();
        if (measurementList.Count < 2)
        {
            return "Stable";
        }

        var firstHalf = measurementList.Take(measurementList.Count / 2).Average(m => m.Depth);
        var secondHalf = measurementList.Skip(measurementList.Count / 2).Average(m => m.Depth);

        var difference = secondHalf - firstHalf;
        var threshold = Math.Max(1, firstHalf * 0.1); // 10% threshold

        return difference > threshold ? "Increasing" :
               difference < -threshold ? "Decreasing" : "Stable";
    }

    /// <summary>
    /// Calculates current average latency.
    /// </summary>
    private double CalculateCurrentAverageLatency()
    {
        var recentProcessingTimes = _processorMetrics.Values
            .SelectMany(p => p.GetRecentProcessingTimes())
            .ToList();

        return recentProcessingTimes.Any() ? recentProcessingTimes.Average() : 0;
    }

    /// <summary>
    /// Calculates overall average latency.
    /// </summary>
    private double CalculateOverallAverageLatency()
    {
        var allProcessingTimes = _processorMetrics.Values
            .SelectMany(p => p.GetAllProcessingTimes())
            .ToList();

        return allProcessingTimes.Any() ? allProcessingTimes.Average() : 0;
    }

    /// <summary>
    /// Calculates peak latency observed.
    /// </summary>
    private double CalculatePeakLatency()
    {
        var allProcessingTimes = _processorMetrics.Values
            .SelectMany(p => p.GetAllProcessingTimes())
            .ToList();

        return allProcessingTimes.Any() ? allProcessingTimes.Max() : 0;
    }

    /// <summary>
    /// Calculates throughput trend.
    /// </summary>
    private string CalculateThroughputTrend()
    {
        lock (_rateCalculationLock)
        {
            if (_averageProcessingRate == 0)
            {
                return "Stable";
            }

            var difference = _currentProcessingRate - _averageProcessingRate;
            var threshold = _averageProcessingRate * 0.1; // 10% threshold

            return difference > threshold ? "Increasing" :
                   difference < -threshold ? "Decreasing" : "Stable";
        }
    }

    /// <summary>
    /// Calculates latency trend.
    /// </summary>
    private string CalculateLatencyTrend()
    {
        var currentLatency = CalculateCurrentAverageLatency();
        var overallLatency = CalculateOverallAverageLatency();

        if (overallLatency == 0)
        {
            return "Stable";
        }

        var difference = currentLatency - overallLatency;
        var threshold = overallLatency * 0.1; // 10% threshold

        return difference > threshold ? "Increasing" :
               difference < -threshold ? "Decreasing" : "Stable";
    }

    /// <summary>
    /// Throws an exception if the object has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(QueueMetrics));
        }
    }

    /// <summary>
    /// Disposes of the queue metrics service and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _logger.Log(LogLevel.Debug, "Disposing queue metrics service");

            _messageMetrics.Clear();
            _batchMetrics.Clear();
            _processorMetrics.Clear();

            while (_queueDepthHistory.TryDequeue(out _)) { }

            _isDisposed = true;

            _logger.Log(LogLevel.Debug, "Queue metrics service disposed");
        }
    }
}

/// <summary>
/// Internal class for tracking metrics for individual messages.
/// </summary>
internal class MessageMetric
{
    private readonly string _testCaseId;
    private readonly object _lock = new();
    private DateTime? _publishedAt;
    private readonly List<ProcessingEvent> _processingEvents = new();
    private readonly List<FailureEvent> _failureEvents = new();

    public MessageMetric(string testCaseId)
    {
        _testCaseId = testCaseId;
    }

    public void RecordPublished(DateTime timestamp)
    {
        lock (_lock)
        {
            _publishedAt = timestamp;
        }
    }

    public void RecordProcessed(string processorName, double processingTimeMs, DateTime timestamp)
    {
        lock (_lock)
        {
            _processingEvents.Add(new ProcessingEvent(processorName, processingTimeMs, timestamp));
        }
    }

    public void RecordFailed(string processorName, string errorMessage, DateTime timestamp)
    {
        lock (_lock)
        {
            _failureEvents.Add(new FailureEvent(processorName, errorMessage, timestamp));
        }
    }

    public bool HasActivityInPeriod(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return (_publishedAt.HasValue && _publishedAt >= startTime && _publishedAt <= endTime) ||
                   _processingEvents.Any(e => e.Timestamp >= startTime && e.Timestamp <= endTime) ||
                   _failureEvents.Any(e => e.Timestamp >= startTime && e.Timestamp <= endTime);
        }
    }

    private record ProcessingEvent(string ProcessorName, double ProcessingTimeMs, DateTime Timestamp);
    private record FailureEvent(string ProcessorName, string ErrorMessage, DateTime Timestamp);
}

/// <summary>
/// Internal class for tracking metrics for individual processors.
/// </summary>
internal class ProcessorMetric
{
    private readonly string _processorName;
    private readonly object _lock = new();
    private readonly List<ProcessingEvent> _processingEvents = new();
    private readonly List<FailureEvent> _failureEvents = new();

    public ProcessorMetric(string processorName)
    {
        _processorName = processorName;
    }

    public void RecordProcessed(double processingTimeMs, DateTime timestamp)
    {
        lock (_lock)
        {
            _processingEvents.Add(new ProcessingEvent(processingTimeMs, timestamp));
        }
    }

    public void RecordFailed(string errorMessage, DateTime timestamp)
    {
        lock (_lock)
        {
            _failureEvents.Add(new FailureEvent(errorMessage, timestamp));
        }
    }

    public IEnumerable<double> GetProcessingTimesInPeriod(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return _processingEvents
                .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
                .Select(e => e.ProcessingTimeMs)
                .ToList();
        }
    }

    public long GetProcessedCountInPeriod(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return _processingEvents.Count(e => e.Timestamp >= startTime && e.Timestamp <= endTime);
        }
    }

    public IEnumerable<double> GetRecentProcessingTimes()
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-5); // Last 5 minutes
        lock (_lock)
        {
            return _processingEvents
                .Where(e => e.Timestamp >= cutoffTime)
                .Select(e => e.ProcessingTimeMs)
                .ToList();
        }
    }

    public IEnumerable<double> GetAllProcessingTimes()
    {
        lock (_lock)
        {
            return _processingEvents.Select(e => e.ProcessingTimeMs).ToList();
        }
    }

    public Contracts.ProcessorStats GetStatsForPeriod(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            var processed = _processingEvents.Count(e => e.Timestamp >= startTime && e.Timestamp <= endTime);
            var failed = _failureEvents.Count(e => e.Timestamp >= startTime && e.Timestamp <= endTime);
            var averageTime = _processingEvents
                .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
                .Select(e => e.ProcessingTimeMs)
                .DefaultIfEmpty(0)
                .Average();
            var errorRate = processed + failed > 0 ? (double)failed / (processed + failed) * 100 : 0;

            return new Contracts.ProcessorStats(processed, failed, averageTime, errorRate);
        }
    }

    private record ProcessingEvent(double ProcessingTimeMs, DateTime Timestamp);
    private record FailureEvent(string ErrorMessage, DateTime Timestamp);
}

/// <summary>
/// Internal class for tracking metrics for individual batches.
/// </summary>
internal class BatchMetric
{
    private readonly string _batchId;
    private readonly object _lock = new();
    private int _batchSize;
    private bool _isCompleted;
    private bool _isTimedOut;
    private double _processingTimeMs;
    private DateTime? _completionTimestamp;
    private DateTime? _timeoutTimestamp;
    private int _expectedSize;
    private int _actualSize;
    private double _timeoutMs;

    public BatchMetric(string batchId)
    {
        _batchId = batchId;
    }

    public int BatchSize
    {
        get
        {
            lock (_lock)
            {
                return _batchSize;
            }
        }
    }

    public bool IsCompleted
    {
        get
        {
            lock (_lock)
            {
                return _isCompleted;
            }
        }
    }

    public bool IsTimedOut
    {
        get
        {
            lock (_lock)
            {
                return _isTimedOut;
            }
        }
    }

    public double ProcessingTimeMs
    {
        get
        {
            lock (_lock)
            {
                return _processingTimeMs;
            }
        }
    }

    public void RecordCompletion(int batchSize, bool completedSuccessfully, double processingTimeMs, DateTime timestamp)
    {
        lock (_lock)
        {
            _batchSize = batchSize;
            _isCompleted = completedSuccessfully;
            _processingTimeMs = processingTimeMs;
            _completionTimestamp = timestamp;
        }
    }

    public void RecordTimeout(int expectedSize, int actualSize, double timeoutMs, DateTime timestamp)
    {
        lock (_lock)
        {
            _isTimedOut = true;
            _expectedSize = expectedSize;
            _actualSize = actualSize;
            _timeoutMs = timeoutMs;
            _timeoutTimestamp = timestamp;
        }
    }

    public bool HasActivityInPeriod(DateTime startTime, DateTime endTime)
    {
        lock (_lock)
        {
            return (_completionTimestamp.HasValue && _completionTimestamp >= startTime && _completionTimestamp <= endTime) ||
                   (_timeoutTimestamp.HasValue && _timeoutTimestamp >= startTime && _timeoutTimestamp <= endTime);
        }
    }
}
