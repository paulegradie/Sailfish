using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Implementation;

/// <summary>
/// Service that optimizes queue system performance and resource usage through dynamic analysis
/// and configuration adjustment. This service is part of the intercepting queue architecture
/// that enables intelligent performance optimization based on real-time metrics and configurable strategies.
/// </summary>
/// <remarks>
/// The QueuePerformanceOptimizer operates as a background optimization service that periodically
/// analyzes queue performance metrics and applies optimizations based on configured strategies.
/// It integrates with the queue health monitoring system to make informed optimization decisions
/// and provides both automatic optimization and manual tuning recommendations.
/// 
/// Key responsibilities:
/// - Monitor queue performance metrics and identify bottlenecks
/// - Implement dynamic queue capacity adjustment based on load
/// - Optimize message processing throughput and latency
/// - Provide performance tuning recommendations
/// - Support configurable optimization strategies and thresholds
/// - Enable adaptive performance tuning based on runtime conditions
/// 
/// The optimizer supports multiple optimization strategies including throughput optimization,
/// latency optimization, memory optimization, balanced optimization, and adaptive optimization
/// that automatically adjusts based on current conditions.
/// 
/// Thread Safety:
/// This implementation is thread-safe and supports concurrent access from multiple threads.
/// All shared state is protected using appropriate synchronization mechanisms to ensure
/// data consistency and prevent race conditions.
/// 
/// Performance Considerations:
/// The performance optimizer is designed to have minimal impact on queue performance.
/// Optimization operations are performed asynchronously and use efficient algorithms
/// to minimize processing overhead and resource usage.
/// </remarks>
public class QueuePerformanceOptimizer : IQueuePerformanceOptimizer, IDisposable
{
    private readonly IQueueHealthCheck _healthCheck;
    private readonly QueueConfiguration _configuration;
    private readonly TestCompletionQueueManager _queueManager;
    private readonly ILogger _logger;
    private readonly object _lock = new();
    
    private Timer? _optimizationTimer;
    private bool _isRunning;
    private bool _isDisposed;
    private DateTime _startTime;
    private OptimizationStrategy _currentStrategy;
    
    // Optimization tracking
    private readonly List<OptimizationResult> _optimizationHistory = new();
    private readonly List<PerformanceBottleneck> _detectedBottlenecks = new();
    private DateTime _lastOptimization = DateTime.MinValue;
    private int _optimizationCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueuePerformanceOptimizer"/> class.
    /// </summary>
    /// <param name="healthCheck">The queue health check service for performance metrics.</param>
    /// <param name="configuration">The queue configuration to optimize.</param>
    /// <param name="queueManager">The queue manager for system integration.</param>
    /// <param name="logger">The logger for optimization events and diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the required parameters are null.
    /// </exception>
    public QueuePerformanceOptimizer(
        IQueueHealthCheck healthCheck,
        QueueConfiguration configuration,
        TestCompletionQueueManager queueManager,
        ILogger logger)
    {
        _healthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _currentStrategy = OptimizationStrategy.Balanced; // Default strategy
    }

    /// <inheritdoc />
    public event EventHandler<OptimizationAppliedEventArgs>? OptimizationApplied;

    /// <inheritdoc />
    public event EventHandler<BottleneckDetectedEventArgs>? BottleneckDetected;

    /// <inheritdoc />
    public OptimizationStrategy CurrentStrategy
    {
        get
        {
            lock (_lock)
            {
                return _currentStrategy;
            }
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Queue performance optimizer is already running. Call StopAsync before starting again.");
            }
        }

        _logger.Log(LogLevel.Information, "Starting queue performance optimizer...");

        try
        {
            // Calculate optimization interval (default to 60 seconds)
            var optimizationInterval = CalculateOptimizationInterval();
            
            // Initialize tracking
            _startTime = DateTime.UtcNow;
            ResetOptimizationHistory();
            
            // Start the optimization timer
            _optimizationTimer = new Timer(
                PerformOptimization,
                null,
                optimizationInterval,
                optimizationInterval);

            lock (_lock)
            {
                _isRunning = true;
            }

            // Perform initial analysis
            await PerformInitialAnalysis(cancellationToken).ConfigureAwait(false);

            _logger.Log(LogLevel.Information,
                "Queue performance optimizer started with strategy '{0}' and interval of {1} seconds",
                _currentStrategy, optimizationInterval.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to start queue performance optimizer: {0}", ex.Message);
            
            // Cleanup on failure
            await CleanupAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        _logger.Log(LogLevel.Information, "Stopping queue performance optimizer...");

        try
        {
            await CleanupAsync().ConfigureAwait(false);

            _logger.Log(LogLevel.Information, 
                "Queue performance optimizer stopped successfully. Applied {0} optimizations during session.",
                _optimizationCount);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Error occurred while stopping queue performance optimizer: {0}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PerformanceAnalysisResult> AnalyzePerformanceAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.Log(LogLevel.Debug, "Performing queue performance analysis...");

            // Get current health metrics
            var metrics = await _healthCheck.GetHealthMetricsAsync(cancellationToken).ConfigureAwait(false);
            
            // Analyze performance and identify bottlenecks
            var bottlenecks = IdentifyBottlenecks(metrics, cancellationToken);
            
            // Calculate overall performance score
            var performanceScore = CalculatePerformanceScore(metrics, bottlenecks);
            
            // Generate recommended actions
            var recommendedActions = GenerateRecommendedActions(bottlenecks, metrics);

            var result = new PerformanceAnalysisResult(
                DateTime.UtcNow,
                performanceScore,
                bottlenecks,
                metrics,
                recommendedActions);

            _logger.Log(LogLevel.Information,
                "Performance analysis completed. Score: {0:F1}/100, Bottlenecks: {1}",
                performanceScore, bottlenecks.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Error occurred during performance analysis: {0}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<OptimizationResult> OptimizeConfigurationAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.Log(LogLevel.Debug, "Optimizing queue configuration with strategy '{0}'...", _currentStrategy);

            // Analyze current performance
            var analysisResult = await AnalyzePerformanceAsync(cancellationToken).ConfigureAwait(false);
            
            // Capture current configuration
            var previousConfig = CaptureCurrentConfiguration();
            
            // Apply optimizations based on strategy
            var appliedOptimizations = await ApplyOptimizations(analysisResult, cancellationToken).ConfigureAwait(false);
            
            // Capture new configuration
            var newConfig = CaptureCurrentConfiguration();
            
            // Create optimization result
            var optimizationResult = new OptimizationResult(
                DateTime.UtcNow,
                _currentStrategy,
                appliedOptimizations,
                GenerateExpectedImpact(appliedOptimizations),
                CalculateConfigurationChanges(previousConfig, newConfig));

            // Track optimization
            lock (_lock)
            {
                _optimizationHistory.Add(optimizationResult);
                _lastOptimization = DateTime.UtcNow;
                _optimizationCount++;
            }

            // Raise event
            OptimizationApplied?.Invoke(this, new OptimizationAppliedEventArgs(
                optimizationResult, previousConfig, newConfig));

            _logger.Log(LogLevel.Information,
                "Configuration optimization completed. Applied {0} optimizations with strategy '{1}'",
                appliedOptimizations.Count, _currentStrategy);

            return optimizationResult;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Error occurred during configuration optimization: {0}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OptimizationRecommendation>> GetOptimizationRecommendationsAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.Log(LogLevel.Debug, "Generating optimization recommendations...");

            // Analyze current performance
            var analysisResult = await AnalyzePerformanceAsync(cancellationToken).ConfigureAwait(false);
            
            // Generate recommendations based on analysis
            var recommendations = GenerateOptimizationRecommendations(analysisResult);

            _logger.Log(LogLevel.Information,
                "Generated {0} optimization recommendations", recommendations.Count);

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Error occurred while generating optimization recommendations: {0}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetOptimizationStrategyAsync(OptimizationStrategy strategy, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.IsDefined(typeof(OptimizationStrategy), strategy))
        {
            throw new ArgumentException($"Unsupported optimization strategy: {strategy}", nameof(strategy));
        }

        var previousStrategy = _currentStrategy;
        
        lock (_lock)
        {
            _currentStrategy = strategy;
        }

        _logger.Log(LogLevel.Information,
            "Optimization strategy changed from '{0}' to '{1}'", previousStrategy, strategy);

        // Trigger immediate re-analysis if running
        if (_isRunning)
        {
            try
            {
                await PerformOptimization(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex,
                    "Error occurred during strategy change optimization: {0}", ex.Message);
            }
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the optimizer has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(QueuePerformanceOptimizer));
        }
    }

    /// <summary>
    /// Calculates the optimization interval based on configuration.
    /// </summary>
    /// <returns>The optimization interval.</returns>
    private TimeSpan CalculateOptimizationInterval()
    {
        // Default to 60 seconds, but could be made configurable
        var defaultInterval = TimeSpan.FromSeconds(60);

        // Could adjust based on queue activity or configuration
        // For now, use a fixed interval
        return defaultInterval;
    }

    /// <summary>
    /// Resets optimization history and tracking.
    /// </summary>
    private void ResetOptimizationHistory()
    {
        lock (_lock)
        {
            _optimizationHistory.Clear();
            _detectedBottlenecks.Clear();
            _lastOptimization = DateTime.MinValue;
            _optimizationCount = 0;
        }
    }

    /// <summary>
    /// Performs initial analysis after startup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task PerformInitialAnalysis(CancellationToken cancellationToken)
    {
        try
        {
            var analysisResult = await AnalyzePerformanceAsync(cancellationToken).ConfigureAwait(false);

            // Log initial performance state
            _logger.Log(LogLevel.Information,
                "Initial performance analysis completed. Score: {0:F1}/100, Strategy: {1}",
                analysisResult.OverallPerformanceScore, _currentStrategy);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Error occurred during initial performance analysis: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Performs periodic optimization (called by timer).
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private async void PerformOptimization(object? state)
    {
        if (_isDisposed || !_isRunning)
            return;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5-minute timeout
            await PerformOptimization(cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Error occurred during periodic optimization: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Performs optimization with cancellation token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task PerformOptimization(CancellationToken cancellationToken)
    {
        try
        {
            // Analyze current performance
            var analysisResult = await AnalyzePerformanceAsync(cancellationToken).ConfigureAwait(false);

            // Check if optimization is needed
            if (ShouldOptimize(analysisResult))
            {
                await OptimizeConfigurationAsync(cancellationToken).ConfigureAwait(false);
            }

            // Check for new bottlenecks
            CheckForBottlenecks(analysisResult, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Error occurred during optimization cycle: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Determines if optimization should be performed based on analysis results.
    /// </summary>
    /// <param name="analysisResult">The performance analysis result.</param>
    /// <returns>True if optimization should be performed; otherwise, false.</returns>
    private bool ShouldOptimize(PerformanceAnalysisResult analysisResult)
    {
        // Don't optimize too frequently
        if (DateTime.UtcNow - _lastOptimization < TimeSpan.FromMinutes(5))
        {
            return false;
        }

        // Optimize if performance score is below threshold
        if (analysisResult.OverallPerformanceScore < 70)
        {
            return true;
        }

        // Optimize if critical bottlenecks are detected
        if (analysisResult.IdentifiedBottlenecks.Any(b => b.Severity == BottleneckSeverity.Critical))
        {
            return true;
        }

        // Optimize if multiple high-severity bottlenecks
        if (analysisResult.IdentifiedBottlenecks.Count(b => b.Severity == BottleneckSeverity.High) >= 2)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks for bottlenecks and raises events if new ones are detected.
    /// </summary>
    /// <param name="analysisResult">The performance analysis result.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private void CheckForBottlenecks(PerformanceAnalysisResult analysisResult, CancellationToken cancellationToken)
    {
        foreach (var bottleneck in analysisResult.IdentifiedBottlenecks)
        {
            // Check if this is a new bottleneck
            bool isNewBottleneck;
            lock (_lock)
            {
                isNewBottleneck = !_detectedBottlenecks.Any(b =>
                    b.Type == bottleneck.Type && b.Severity == bottleneck.Severity);

                if (isNewBottleneck)
                {
                    _detectedBottlenecks.Add(bottleneck);
                }
            }

            if (isNewBottleneck)
            {
                var recommendedActions = GenerateBottleneckActions(bottleneck);

                BottleneckDetected?.Invoke(this, new BottleneckDetectedEventArgs(
                    bottleneck, analysisResult.PerformanceMetrics, recommendedActions));

                _logger.Log(LogLevel.Warning,
                    "Performance bottleneck detected: {0} (Severity: {1})",
                    bottleneck.Description, bottleneck.Severity);
            }
        }
    }

    /// <summary>
    /// Cleans up resources and stops optimization.
    /// </summary>
    private async Task CleanupAsync()
    {
        lock (_lock)
        {
            _isRunning = false;
        }

        if (_optimizationTimer != null)
        {
            await _optimizationTimer.DisposeAsync().ConfigureAwait(false);
            _optimizationTimer = null;
        }
    }

    /// <summary>
    /// Identifies performance bottlenecks based on metrics.
    /// </summary>
    /// <param name="metrics">The performance metrics to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of identified bottlenecks.</returns>
    private IReadOnlyList<PerformanceBottleneck> IdentifyBottlenecks(
        QueueHealthMetrics metrics, CancellationToken cancellationToken)
    {
        var bottlenecks = new List<PerformanceBottleneck>();

        // Check queue depth bottlenecks
        if (metrics.QueueDepth > _configuration.MaxQueueCapacity * 0.9)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "QueueCapacity",
                BottleneckSeverity.Critical,
                $"Queue depth ({metrics.QueueDepth}) is near capacity ({_configuration.MaxQueueCapacity})",
                new[] { "QueueDepth", "ProcessingRate" },
                "Increase queue capacity or improve processing speed"));
        }
        else if (metrics.QueueDepth > _configuration.MaxQueueCapacity * 0.7)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "QueueCapacity",
                BottleneckSeverity.High,
                $"Queue depth ({metrics.QueueDepth}) is approaching capacity",
                new[] { "QueueDepth" },
                "Monitor queue depth and consider capacity increase"));
        }

        // Check processing speed bottlenecks
        if (metrics.MessagesProcessedPerSecond < 1.0)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "ProcessingSpeed",
                BottleneckSeverity.High,
                $"Low processing rate ({metrics.MessagesProcessedPerSecond:F2} msg/sec)",
                new[] { "MessagesProcessedPerSecond", "AverageProcessingTimeMs" },
                "Optimize processor performance or reduce processing timeout"));
        }

        // Check error rate bottlenecks
        if (metrics.ErrorRate > 10)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "ErrorRate",
                BottleneckSeverity.Critical,
                $"High error rate ({metrics.ErrorRate:F1}%)",
                new[] { "ErrorRate", "RetryRate" },
                "Investigate and fix underlying errors"));
        }
        else if (metrics.ErrorRate > 5)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "ErrorRate",
                BottleneckSeverity.Medium,
                $"Elevated error rate ({metrics.ErrorRate:F1}%)",
                new[] { "ErrorRate" },
                "Monitor error patterns and consider error handling improvements"));
        }

        // Check batch timeout bottlenecks
        if (metrics.BatchTimeoutRate > 20)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "BatchTimeout",
                BottleneckSeverity.High,
                $"High batch timeout rate ({metrics.BatchTimeoutRate:F1}%)",
                new[] { "BatchTimeoutRate", "BatchCompletionRate" },
                "Increase batch timeout or optimize batch processing"));
        }

        // Check processing time bottlenecks
        if (metrics.AverageProcessingTimeMs > _configuration.ProcessingTimeoutMs * 0.8)
        {
            bottlenecks.Add(new PerformanceBottleneck(
                "ProcessingTime",
                BottleneckSeverity.Medium,
                $"High processing time ({metrics.AverageProcessingTimeMs:F0}ms)",
                new[] { "AverageProcessingTimeMs" },
                "Optimize processor logic or increase processing timeout"));
        }

        return bottlenecks;
    }

    /// <summary>
    /// Calculates overall performance score based on metrics and bottlenecks.
    /// </summary>
    /// <param name="metrics">The performance metrics.</param>
    /// <param name="bottlenecks">The identified bottlenecks.</param>
    /// <returns>Performance score from 0-100.</returns>
    private double CalculatePerformanceScore(QueueHealthMetrics metrics, IReadOnlyList<PerformanceBottleneck> bottlenecks)
    {
        double score = 100.0;

        // Deduct points for bottlenecks
        foreach (var bottleneck in bottlenecks)
        {
            score -= bottleneck.Severity switch
            {
                BottleneckSeverity.Critical => 25,
                BottleneckSeverity.High => 15,
                BottleneckSeverity.Medium => 10,
                BottleneckSeverity.Low => 5,
                _ => 0
            };
        }

        // Deduct points for poor metrics
        if (metrics.ErrorRate > 0)
        {
            score -= Math.Min(metrics.ErrorRate * 2, 20); // Max 20 points for errors
        }

        if (metrics.BatchTimeoutRate > 0)
        {
            score -= Math.Min(metrics.BatchTimeoutRate, 15); // Max 15 points for timeouts
        }

        // Bonus points for good performance
        if (metrics.MessagesProcessedPerSecond > 10)
        {
            score += 5; // Bonus for high throughput
        }

        if (metrics.ErrorRate == 0 && metrics.BatchTimeoutRate == 0)
        {
            score += 10; // Bonus for zero errors and timeouts
        }

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Generates recommended actions based on bottlenecks and metrics.
    /// </summary>
    /// <param name="bottlenecks">The identified bottlenecks.</param>
    /// <param name="metrics">The performance metrics.</param>
    /// <returns>List of recommended actions.</returns>
    private IReadOnlyList<string> GenerateRecommendedActions(
        IReadOnlyList<PerformanceBottleneck> bottlenecks, QueueHealthMetrics metrics)
    {
        var actions = new List<string>();

        if (bottlenecks.Any(b => b.Type == "QueueCapacity"))
        {
            actions.Add("Consider increasing queue capacity to handle peak loads");
        }

        if (bottlenecks.Any(b => b.Type == "ProcessingSpeed"))
        {
            actions.Add("Optimize processor performance or increase processing timeout");
        }

        if (bottlenecks.Any(b => b.Type == "ErrorRate"))
        {
            actions.Add("Investigate and resolve underlying errors causing processing failures");
        }

        if (bottlenecks.Any(b => b.Type == "BatchTimeout"))
        {
            actions.Add("Increase batch completion timeout or optimize batch processing logic");
        }

        if (metrics.QueueDepth > metrics.AverageQueueDepth * 2)
        {
            actions.Add("Current queue depth is significantly above average - monitor for load spikes");
        }

        if (actions.Count == 0)
        {
            actions.Add("Performance is within acceptable parameters - continue monitoring");
        }

        return actions;
    }

    /// <summary>
    /// Captures the current configuration for comparison.
    /// </summary>
    /// <returns>Dictionary containing current configuration values.</returns>
    private Dictionary<string, object> CaptureCurrentConfiguration()
    {
        return new Dictionary<string, object>
        {
            ["MaxQueueCapacity"] = _configuration.MaxQueueCapacity,
            ["PublishTimeoutMs"] = _configuration.PublishTimeoutMs,
            ["ProcessingTimeoutMs"] = _configuration.ProcessingTimeoutMs,
            ["BatchCompletionTimeoutMs"] = _configuration.BatchCompletionTimeoutMs,
            ["MaxBatchSize"] = _configuration.MaxBatchSize,
            ["MaxRetryAttempts"] = _configuration.MaxRetryAttempts,
            ["BaseRetryDelayMs"] = _configuration.BaseRetryDelayMs,
            ["EnableBatchProcessing"] = _configuration.EnableBatchProcessing
        };
    }

    /// <summary>
    /// Applies optimizations based on analysis results and strategy.
    /// </summary>
    /// <param name="analysisResult">The performance analysis result.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>List of applied optimizations.</returns>
    private Task<IReadOnlyList<string>> ApplyOptimizations(
        PerformanceAnalysisResult analysisResult, CancellationToken cancellationToken)
    {
        var appliedOptimizations = new List<string>();

        foreach (var bottleneck in analysisResult.IdentifiedBottlenecks)
        {
            var optimizations = ApplyBottleneckOptimization(bottleneck);
            appliedOptimizations.AddRange(optimizations);
        }

        // Apply strategy-specific optimizations
        var strategyOptimizations = ApplyStrategyOptimizations(analysisResult.PerformanceMetrics);
        appliedOptimizations.AddRange(strategyOptimizations);

        return Task.FromResult<IReadOnlyList<string>>(appliedOptimizations);
    }

    /// <summary>
    /// Applies optimizations for a specific bottleneck.
    /// </summary>
    /// <param name="bottleneck">The bottleneck to optimize.</param>
    /// <returns>List of applied optimizations.</returns>
    private IReadOnlyList<string> ApplyBottleneckOptimization(PerformanceBottleneck bottleneck)
    {
        var optimizations = new List<string>();

        switch (bottleneck.Type)
        {
            case "QueueCapacity":
                if (bottleneck.Severity >= BottleneckSeverity.High)
                {
                    var newCapacity = (int)(_configuration.MaxQueueCapacity * 1.5);
                    _configuration.MaxQueueCapacity = newCapacity;
                    optimizations.Add($"Increased queue capacity to {newCapacity}");
                }
                break;

            case "ProcessingSpeed":
                if (bottleneck.Severity >= BottleneckSeverity.High)
                {
                    var newTimeout = (int)(_configuration.ProcessingTimeoutMs * 1.2);
                    _configuration.ProcessingTimeoutMs = newTimeout;
                    optimizations.Add($"Increased processing timeout to {newTimeout}ms");
                }
                break;

            case "BatchTimeout":
                if (bottleneck.Severity >= BottleneckSeverity.Medium)
                {
                    var newTimeout = (int)(_configuration.BatchCompletionTimeoutMs * 1.3);
                    _configuration.BatchCompletionTimeoutMs = newTimeout;
                    optimizations.Add($"Increased batch completion timeout to {newTimeout}ms");
                }
                break;

            case "ErrorRate":
                if (bottleneck.Severity >= BottleneckSeverity.Medium)
                {
                    var newRetryAttempts = Math.Min(_configuration.MaxRetryAttempts + 1, 5);
                    _configuration.MaxRetryAttempts = newRetryAttempts;
                    optimizations.Add($"Increased retry attempts to {newRetryAttempts}");
                }
                break;
        }

        return optimizations;
    }

    /// <summary>
    /// Applies strategy-specific optimizations.
    /// </summary>
    /// <param name="metrics">The performance metrics.</param>
    /// <returns>List of applied optimizations.</returns>
    private IReadOnlyList<string> ApplyStrategyOptimizations(QueueHealthMetrics metrics)
    {
        var optimizations = new List<string>();

        switch (_currentStrategy)
        {
            case OptimizationStrategy.Throughput:
                optimizations.AddRange(ApplyThroughputOptimizations(metrics));
                break;

            case OptimizationStrategy.Latency:
                optimizations.AddRange(ApplyLatencyOptimizations(metrics));
                break;

            case OptimizationStrategy.Memory:
                optimizations.AddRange(ApplyMemoryOptimizations(metrics));
                break;

            case OptimizationStrategy.Balanced:
                optimizations.AddRange(ApplyBalancedOptimizations(metrics));
                break;

            case OptimizationStrategy.Adaptive:
                optimizations.AddRange(ApplyAdaptiveOptimizations(metrics));
                break;
        }

        return optimizations;
    }

    /// <summary>
    /// Applies throughput-focused optimizations.
    /// </summary>
    /// <param name="metrics">The performance metrics.</param>
    /// <returns>List of applied optimizations.</returns>
    private IReadOnlyList<string> ApplyThroughputOptimizations(QueueHealthMetrics metrics)
    {
        var optimizations = new List<string>();

        // Increase queue capacity for better throughput
        if (metrics.QueueDepth > _configuration.MaxQueueCapacity * 0.6)
        {
            var newCapacity = (int)(_configuration.MaxQueueCapacity * 1.3);
            _configuration.MaxQueueCapacity = newCapacity;
            optimizations.Add($"Increased queue capacity to {newCapacity} for better throughput");
        }

        // Increase batch size for better throughput
        if (_configuration.MaxBatchSize < 100)
        {
            var newBatchSize = Math.Min(_configuration.MaxBatchSize + 20, 100);
            _configuration.MaxBatchSize = newBatchSize;
            optimizations.Add($"Increased batch size to {newBatchSize} for better throughput");
        }

        return optimizations;
    }

    /// <summary>
    /// Applies latency-focused optimizations.
    /// </summary>
    /// <param name="metrics">The performance metrics.</param>
    /// <returns>List of applied optimizations.</returns>
    private IReadOnlyList<string> ApplyLatencyOptimizations(QueueHealthMetrics metrics)
    {
        var optimizations = new List<string>();

        // Reduce batch timeout for lower latency
        if (_configuration.BatchCompletionTimeoutMs > 30000)
        {
            var newTimeout = Math.Max(_configuration.BatchCompletionTimeoutMs - 10000, 30000);
            _configuration.BatchCompletionTimeoutMs = newTimeout;
            optimizations.Add($"Reduced batch timeout to {newTimeout}ms for lower latency");
        }

        // Reduce batch size for lower latency
        if (_configuration.MaxBatchSize > 20)
        {
            var newBatchSize = Math.Max(_configuration.MaxBatchSize - 10, 20);
            _configuration.MaxBatchSize = newBatchSize;
            optimizations.Add($"Reduced batch size to {newBatchSize} for lower latency");
        }

        return optimizations;
    }

    /// <summary>
    /// Applies memory-focused optimizations.
    /// </summary>
    /// <param name="metrics">The performance metrics.</param>
    /// <returns>List of applied optimizations.</returns>
    private IReadOnlyList<string> ApplyMemoryOptimizations(QueueHealthMetrics metrics)
    {
        var optimizations = new List<string>();

        // Reduce queue capacity for lower memory usage
        if (_configuration.MaxQueueCapacity > 500)
        {
            var newCapacity = Math.Max(_configuration.MaxQueueCapacity - 200, 500);
            _configuration.MaxQueueCapacity = newCapacity;
            optimizations.Add($"Reduced queue capacity to {newCapacity} for lower memory usage");
        }

        // Reduce batch size for lower memory usage
        if (_configuration.MaxBatchSize > 25)
        {
            var newBatchSize = Math.Max(_configuration.MaxBatchSize - 15, 25);
            _configuration.MaxBatchSize = newBatchSize;
            optimizations.Add($"Reduced batch size to {newBatchSize} for lower memory usage");
        }

        return optimizations;
    }

    /// <summary>
    /// Applies balanced optimizations.
    /// </summary>
    /// <param name="metrics">The performance metrics.</param>
    /// <returns>List of applied optimizations.</returns>
    private IReadOnlyList<string> ApplyBalancedOptimizations(QueueHealthMetrics metrics)
    {
        var optimizations = new List<string>();

        // Apply moderate adjustments based on current performance
        if (metrics.MessagesProcessedPerSecond < 5 && metrics.QueueDepth > _configuration.MaxQueueCapacity * 0.7)
        {
            // Increase capacity moderately
            var newCapacity = (int)(_configuration.MaxQueueCapacity * 1.2);
            _configuration.MaxQueueCapacity = newCapacity;
            optimizations.Add($"Increased queue capacity to {newCapacity} for balanced performance");
        }

        if (metrics.AverageProcessingTimeMs > _configuration.ProcessingTimeoutMs * 0.6)
        {
            // Increase timeout moderately
            var newTimeout = (int)(_configuration.ProcessingTimeoutMs * 1.1);
            _configuration.ProcessingTimeoutMs = newTimeout;
            optimizations.Add($"Increased processing timeout to {newTimeout}ms for balanced performance");
        }

        return optimizations;
    }

    /// <summary>
    /// Applies adaptive optimizations based on current conditions.
    /// </summary>
    /// <param name="metrics">The performance metrics.</param>
    /// <returns>List of applied optimizations.</returns>
    private IReadOnlyList<string> ApplyAdaptiveOptimizations(QueueHealthMetrics metrics)
    {
        var optimizations = new List<string>();

        // Determine current system state and apply appropriate strategy
        if (metrics.MessagesProcessedPerSecond < 2)
        {
            // System is slow - apply throughput optimizations
            optimizations.AddRange(ApplyThroughputOptimizations(metrics));
            optimizations.Add("Applied throughput optimizations due to low processing rate");
        }
        else if (metrics.AverageProcessingTimeMs > _configuration.ProcessingTimeoutMs * 0.7)
        {
            // System has high latency - apply latency optimizations
            optimizations.AddRange(ApplyLatencyOptimizations(metrics));
            optimizations.Add("Applied latency optimizations due to high processing time");
        }
        else if (metrics.QueueDepth > _configuration.MaxQueueCapacity * 0.8)
        {
            // System is memory constrained - apply memory optimizations
            optimizations.AddRange(ApplyMemoryOptimizations(metrics));
            optimizations.Add("Applied memory optimizations due to high queue depth");
        }
        else
        {
            // System is performing well - apply balanced optimizations
            optimizations.AddRange(ApplyBalancedOptimizations(metrics));
            optimizations.Add("Applied balanced optimizations for stable performance");
        }

        return optimizations;
    }

    /// <summary>
    /// Generates expected impact description for applied optimizations.
    /// </summary>
    /// <param name="appliedOptimizations">The applied optimizations.</param>
    /// <returns>Expected impact description.</returns>
    private string GenerateExpectedImpact(IReadOnlyList<string> appliedOptimizations)
    {
        if (appliedOptimizations.Count == 0)
        {
            return "No optimizations applied - performance is within acceptable parameters";
        }

        var impacts = new List<string>();

        if (appliedOptimizations.Any(o => o.Contains("queue capacity")))
        {
            impacts.Add("improved queue throughput");
        }

        if (appliedOptimizations.Any(o => o.Contains("timeout")))
        {
            impacts.Add("reduced processing failures");
        }

        if (appliedOptimizations.Any(o => o.Contains("batch")))
        {
            impacts.Add("optimized batch processing");
        }

        if (appliedOptimizations.Any(o => o.Contains("retry")))
        {
            impacts.Add("improved error recovery");
        }

        return impacts.Count > 0
            ? $"Expected improvements: {string.Join(", ", impacts)}"
            : "Minor performance improvements expected";
    }

    /// <summary>
    /// Calculates configuration changes between two configurations.
    /// </summary>
    /// <param name="previousConfig">The previous configuration.</param>
    /// <param name="newConfig">The new configuration.</param>
    /// <returns>Dictionary of configuration changes.</returns>
    private Dictionary<string, object> CalculateConfigurationChanges(
        Dictionary<string, object> previousConfig, Dictionary<string, object> newConfig)
    {
        var changes = new Dictionary<string, object>();

        foreach (var kvp in newConfig)
        {
            if (previousConfig.TryGetValue(kvp.Key, out var previousValue))
            {
                if (!Equals(previousValue, kvp.Value))
                {
                    changes[kvp.Key] = new { Previous = previousValue, New = kvp.Value };
                }
            }
            else
            {
                changes[kvp.Key] = new { Previous = (object?)null, New = kvp.Value };
            }
        }

        return changes;
    }

    /// <summary>
    /// Generates optimization recommendations based on analysis results.
    /// </summary>
    /// <param name="analysisResult">The performance analysis result.</param>
    /// <returns>List of optimization recommendations.</returns>
    private IReadOnlyList<OptimizationRecommendation> GenerateOptimizationRecommendations(
        PerformanceAnalysisResult analysisResult)
    {
        var recommendations = new List<OptimizationRecommendation>();

        foreach (var bottleneck in analysisResult.IdentifiedBottlenecks)
        {
            var recommendation = CreateBottleneckRecommendation(bottleneck);
            recommendations.Add(recommendation);
        }

        // Add general performance recommendations
        if (analysisResult.OverallPerformanceScore < 80)
        {
            recommendations.Add(new OptimizationRecommendation(
                RecommendationPriority.Medium,
                "General",
                "Overall performance score is below optimal threshold",
                "Implementing recommended optimizations should improve overall performance",
                new Dictionary<string, object> { ["PerformanceScore"] = analysisResult.OverallPerformanceScore },
                ImplementationEffort.Medium));
        }

        return recommendations;
    }

    /// <summary>
    /// Creates a recommendation for a specific bottleneck.
    /// </summary>
    /// <param name="bottleneck">The bottleneck to create a recommendation for.</param>
    /// <returns>The optimization recommendation.</returns>
    private OptimizationRecommendation CreateBottleneckRecommendation(PerformanceBottleneck bottleneck)
    {
        var priority = bottleneck.Severity switch
        {
            BottleneckSeverity.Critical => RecommendationPriority.Critical,
            BottleneckSeverity.High => RecommendationPriority.High,
            BottleneckSeverity.Medium => RecommendationPriority.Medium,
            BottleneckSeverity.Low => RecommendationPriority.Low,
            _ => RecommendationPriority.Low
        };

        var effort = bottleneck.Type switch
        {
            "QueueCapacity" => ImplementationEffort.Low,
            "ProcessingSpeed" => ImplementationEffort.Medium,
            "BatchTimeout" => ImplementationEffort.Low,
            "ErrorRate" => ImplementationEffort.High,
            _ => ImplementationEffort.Medium
        };

        var configChanges = new Dictionary<string, object>();

        switch (bottleneck.Type)
        {
            case "QueueCapacity":
                configChanges["MaxQueueCapacity"] = (int)(_configuration.MaxQueueCapacity * 1.5);
                break;
            case "ProcessingSpeed":
                configChanges["ProcessingTimeoutMs"] = (int)(_configuration.ProcessingTimeoutMs * 1.2);
                break;
            case "BatchTimeout":
                configChanges["BatchCompletionTimeoutMs"] = (int)(_configuration.BatchCompletionTimeoutMs * 1.3);
                break;
            case "ErrorRate":
                configChanges["MaxRetryAttempts"] = Math.Min(_configuration.MaxRetryAttempts + 1, 5);
                break;
        }

        return new OptimizationRecommendation(
            priority,
            bottleneck.Type,
            bottleneck.Description,
            bottleneck.SuggestedResolution,
            configChanges,
            effort);
    }

    /// <summary>
    /// Generates recommended actions for a specific bottleneck.
    /// </summary>
    /// <param name="bottleneck">The bottleneck to generate actions for.</param>
    /// <returns>List of recommended actions.</returns>
    private IReadOnlyList<string> GenerateBottleneckActions(PerformanceBottleneck bottleneck)
    {
        var actions = new List<string> { bottleneck.SuggestedResolution };

        switch (bottleneck.Type)
        {
            case "QueueCapacity":
                actions.Add("Monitor queue depth trends");
                actions.Add("Consider load balancing if applicable");
                break;
            case "ProcessingSpeed":
                actions.Add("Profile processor performance");
                actions.Add("Check for resource constraints");
                break;
            case "ErrorRate":
                actions.Add("Review error logs for patterns");
                actions.Add("Implement additional error handling");
                break;
            case "BatchTimeout":
                actions.Add("Analyze batch completion patterns");
                actions.Add("Consider batch size optimization");
                break;
        }

        return actions;
    }

    /// <summary>
    /// Disposes the performance optimizer and releases resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the performance optimizer and releases resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
        {
            try
            {
                // Stop the optimizer if running
                if (_isRunning)
                {
                    CleanupAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex,
                    "Error occurred during performance optimizer disposal: {0}", ex.Message);
            }
            finally
            {
                _isDisposed = true;
            }
        }
    }
}
