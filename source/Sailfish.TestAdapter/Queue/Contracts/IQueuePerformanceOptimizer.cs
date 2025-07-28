using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for a service that optimizes queue system performance and resource usage.
/// This interface is part of the intercepting queue architecture that enables dynamic performance
/// optimization based on real-time metrics and configurable optimization strategies.
/// </summary>
/// <remarks>
/// The IQueuePerformanceOptimizer provides intelligent optimization capabilities for the queue
/// infrastructure, including performance monitoring, bottleneck identification, dynamic configuration
/// adjustment, and performance tuning recommendations. This enables the queue system to adapt
/// to varying workloads and optimize for different performance characteristics.
/// 
/// Key responsibilities:
/// - Monitor queue performance metrics and identify bottlenecks
/// - Implement dynamic queue capacity adjustment based on load
/// - Optimize message processing throughput and latency
/// - Provide performance tuning recommendations
/// - Support configurable optimization strategies and thresholds
/// - Enable adaptive performance tuning based on runtime conditions
/// 
/// The performance optimizer operates as a background optimization service that periodically
/// analyzes queue performance and applies optimizations based on configured strategies.
/// It integrates with the queue health monitoring system to make informed optimization decisions.
/// 
/// Thread Safety:
/// All methods in this interface must be thread-safe and support concurrent access from
/// multiple threads. Implementations should use appropriate synchronization mechanisms
/// to ensure data consistency and prevent race conditions.
/// 
/// Performance Considerations:
/// Optimization operations should be efficient and non-blocking to avoid impacting queue
/// performance. The optimizer should use minimal resources and perform optimizations
/// asynchronously where possible.
/// </remarks>
public interface IQueuePerformanceOptimizer
{
    /// <summary>
    /// Starts the performance optimization service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the performance optimizer is already running.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method should be called during queue system startup to begin performance
    /// optimization. The service will start periodic performance analysis and begin
    /// applying optimizations based on the configured strategy.
    /// </remarks>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the performance optimization service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method should be called during queue system shutdown to stop performance
    /// optimization. The service will complete any in-progress optimizations and
    /// clean up resources.
    /// </remarks>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Analyzes current queue performance and identifies bottlenecks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous analysis operation, containing performance analysis results.</returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method performs comprehensive analysis of queue performance metrics to identify
    /// bottlenecks, inefficiencies, and optimization opportunities. The analysis includes
    /// queue depth patterns, processing rates, error rates, and resource utilization.
    /// </remarks>
    Task<PerformanceAnalysisResult> AnalyzePerformanceAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Optimizes queue configuration based on current performance metrics and optimization strategy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous optimization operation, containing optimization results.</returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method applies dynamic optimizations to the queue configuration based on current
    /// performance metrics and the configured optimization strategy. Optimizations may include
    /// adjusting queue capacity, timeout values, batch sizes, and other performance-related settings.
    /// </remarks>
    Task<OptimizationResult> OptimizeConfigurationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets performance tuning recommendations based on current metrics and analysis.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing performance recommendations.</returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides actionable recommendations for improving queue performance
    /// based on analysis of current metrics and performance patterns. Recommendations
    /// include specific configuration changes and their expected impact.
    /// </remarks>
    Task<IReadOnlyList<OptimizationRecommendation>> GetOptimizationRecommendationsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current optimization strategy being used.
    /// </summary>
    /// <value>The current optimization strategy.</value>
    /// <remarks>
    /// The optimization strategy determines how the optimizer balances different performance
    /// characteristics such as throughput, latency, and memory usage.
    /// </remarks>
    OptimizationStrategy CurrentStrategy { get; }

    /// <summary>
    /// Sets the optimization strategy to use for performance optimization.
    /// </summary>
    /// <param name="strategy">The optimization strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="System.ArgumentException">
    /// Thrown when the specified strategy is not supported.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// Changing the optimization strategy will affect how future optimizations are applied.
    /// The change takes effect immediately and may trigger a re-analysis of current performance.
    /// </remarks>
    Task SetOptimizationStrategyAsync(OptimizationStrategy strategy, CancellationToken cancellationToken);

    /// <summary>
    /// Occurs when a performance optimization is applied.
    /// </summary>
    /// <remarks>
    /// This event is raised whenever the optimizer applies a performance optimization
    /// to the queue configuration. Subscribers can use this event to track optimization
    /// activities and their impact on system performance.
    /// </remarks>
    event EventHandler<OptimizationAppliedEventArgs>? OptimizationApplied;

    /// <summary>
    /// Occurs when a performance bottleneck is detected.
    /// </summary>
    /// <remarks>
    /// This event is raised when the optimizer detects a performance bottleneck that
    /// may require attention. Subscribers can use this event to implement alerting
    /// or other reactive monitoring behaviors.
    /// </remarks>
    event EventHandler<BottleneckDetectedEventArgs>? BottleneckDetected;
}

/// <summary>
/// Defines the optimization strategies available for queue performance optimization.
/// </summary>
/// <remarks>
/// Each strategy represents a different approach to balancing performance characteristics
/// such as throughput, latency, and memory usage. The optimizer will apply different
/// optimization algorithms based on the selected strategy.
/// </remarks>
public enum OptimizationStrategy
{
    /// <summary>
    /// Optimize for maximum message processing throughput.
    /// This strategy prioritizes processing as many messages as possible per unit time,
    /// potentially at the cost of increased latency and memory usage.
    /// </summary>
    Throughput,

    /// <summary>
    /// Optimize for minimum processing latency.
    /// This strategy prioritizes reducing the time between message arrival and processing,
    /// potentially at the cost of reduced throughput and increased resource usage.
    /// </summary>
    Latency,

    /// <summary>
    /// Optimize for minimum memory usage.
    /// This strategy prioritizes reducing memory consumption,
    /// potentially at the cost of reduced throughput and increased latency.
    /// </summary>
    Memory,

    /// <summary>
    /// Balanced optimization across throughput, latency, and memory.
    /// This strategy attempts to find an optimal balance between all performance
    /// characteristics without heavily favoring any single aspect.
    /// </summary>
    Balanced,

    /// <summary>
    /// Adaptive optimization that changes strategy based on current conditions.
    /// This strategy monitors system conditions and automatically adjusts the
    /// optimization approach based on current workload and performance patterns.
    /// </summary>
    Adaptive
}

/// <summary>
/// Represents the result of a performance analysis operation.
/// </summary>
/// <param name="Timestamp">The timestamp when the analysis was performed.</param>
/// <param name="OverallPerformanceScore">Overall performance score from 0-100 (higher is better).</param>
/// <param name="IdentifiedBottlenecks">List of identified performance bottlenecks.</param>
/// <param name="PerformanceMetrics">Current performance metrics used in the analysis.</param>
/// <param name="RecommendedActions">Recommended actions to improve performance.</param>
/// <remarks>
/// This record provides comprehensive results from performance analysis including
/// identified bottlenecks, current metrics, and recommended optimization actions.
/// </remarks>
public record PerformanceAnalysisResult(
    DateTime Timestamp,
    double OverallPerformanceScore,
    IReadOnlyList<PerformanceBottleneck> IdentifiedBottlenecks,
    QueueHealthMetrics PerformanceMetrics,
    IReadOnlyList<string> RecommendedActions
);

/// <summary>
/// Represents the result of an optimization operation.
/// </summary>
/// <param name="Timestamp">The timestamp when the optimization was applied.</param>
/// <param name="Strategy">The optimization strategy that was used.</param>
/// <param name="AppliedOptimizations">List of optimizations that were applied.</param>
/// <param name="ExpectedImpact">Expected impact of the optimizations.</param>
/// <param name="ConfigurationChanges">Configuration changes that were made.</param>
/// <remarks>
/// This record provides details about optimizations that were applied including
/// the strategy used, specific changes made, and expected performance impact.
/// </remarks>
public record OptimizationResult(
    DateTime Timestamp,
    OptimizationStrategy Strategy,
    IReadOnlyList<string> AppliedOptimizations,
    string ExpectedImpact,
    Dictionary<string, object> ConfigurationChanges
);

/// <summary>
/// Represents a performance optimization recommendation.
/// </summary>
/// <param name="Priority">Priority level of the recommendation (High, Medium, Low).</param>
/// <param name="Category">Category of the optimization (Capacity, Timeout, Batching, etc.).</param>
/// <param name="Description">Human-readable description of the recommendation.</param>
/// <param name="ExpectedImpact">Expected impact of implementing the recommendation.</param>
/// <param name="ConfigurationChanges">Specific configuration changes recommended.</param>
/// <param name="EstimatedEffort">Estimated effort to implement (Low, Medium, High).</param>
/// <remarks>
/// This record provides actionable recommendations for improving queue performance
/// including priority, expected impact, and specific implementation details.
/// </remarks>
public record OptimizationRecommendation(
    RecommendationPriority Priority,
    string Category,
    string Description,
    string ExpectedImpact,
    Dictionary<string, object> ConfigurationChanges,
    ImplementationEffort EstimatedEffort
);

/// <summary>
/// Represents a detected performance bottleneck.
/// </summary>
/// <param name="Type">Type of bottleneck (QueueCapacity, ProcessingSpeed, BatchTimeout, etc.).</param>
/// <param name="Severity">Severity level of the bottleneck (Low, Medium, High, Critical).</param>
/// <param name="Description">Human-readable description of the bottleneck.</param>
/// <param name="AffectedMetrics">Metrics that are affected by this bottleneck.</param>
/// <param name="SuggestedResolution">Suggested resolution for the bottleneck.</param>
/// <remarks>
/// This record provides details about identified performance bottlenecks including
/// their type, severity, impact, and suggested resolution approaches.
/// </remarks>
public record PerformanceBottleneck(
    string Type,
    BottleneckSeverity Severity,
    string Description,
    IReadOnlyList<string> AffectedMetrics,
    string SuggestedResolution
);

/// <summary>
/// Event arguments for optimization applied events.
/// </summary>
/// <param name="OptimizationResult">The result of the optimization that was applied.</param>
/// <param name="PreviousConfiguration">The configuration before optimization.</param>
/// <param name="NewConfiguration">The configuration after optimization.</param>
/// <remarks>
/// This class provides event data when a performance optimization is applied,
/// including details about the optimization and configuration changes.
/// </remarks>
public class OptimizationAppliedEventArgs : EventArgs
{
    public OptimizationResult OptimizationResult { get; }
    public Dictionary<string, object> PreviousConfiguration { get; }
    public Dictionary<string, object> NewConfiguration { get; }

    public OptimizationAppliedEventArgs(
        OptimizationResult optimizationResult,
        Dictionary<string, object> previousConfiguration,
        Dictionary<string, object> newConfiguration)
    {
        OptimizationResult = optimizationResult ?? throw new ArgumentNullException(nameof(optimizationResult));
        PreviousConfiguration = previousConfiguration ?? throw new ArgumentNullException(nameof(previousConfiguration));
        NewConfiguration = newConfiguration ?? throw new ArgumentNullException(nameof(newConfiguration));
    }
}

/// <summary>
/// Event arguments for bottleneck detected events.
/// </summary>
/// <param name="Bottleneck">The detected performance bottleneck.</param>
/// <param name="CurrentMetrics">Current performance metrics when bottleneck was detected.</param>
/// <param name="RecommendedActions">Recommended actions to address the bottleneck.</param>
/// <remarks>
/// This class provides event data when a performance bottleneck is detected,
/// including details about the bottleneck and recommended resolution actions.
/// </remarks>
public class BottleneckDetectedEventArgs : EventArgs
{
    public PerformanceBottleneck Bottleneck { get; }
    public QueueHealthMetrics CurrentMetrics { get; }
    public IReadOnlyList<string> RecommendedActions { get; }

    public BottleneckDetectedEventArgs(
        PerformanceBottleneck bottleneck,
        QueueHealthMetrics currentMetrics,
        IReadOnlyList<string> recommendedActions)
    {
        Bottleneck = bottleneck ?? throw new ArgumentNullException(nameof(bottleneck));
        CurrentMetrics = currentMetrics ?? throw new ArgumentNullException(nameof(currentMetrics));
        RecommendedActions = recommendedActions ?? throw new ArgumentNullException(nameof(recommendedActions));
    }
}

/// <summary>
/// Defines the priority levels for optimization recommendations.
/// </summary>
public enum RecommendationPriority
{
    /// <summary>Low priority recommendation that can be implemented when convenient.</summary>
    Low,
    /// <summary>Medium priority recommendation that should be considered for implementation.</summary>
    Medium,
    /// <summary>High priority recommendation that should be implemented soon.</summary>
    High,
    /// <summary>Critical priority recommendation that should be implemented immediately.</summary>
    Critical
}

/// <summary>
/// Defines the estimated effort levels for implementing recommendations.
/// </summary>
public enum ImplementationEffort
{
    /// <summary>Low effort - can be implemented with minimal configuration changes.</summary>
    Low,
    /// <summary>Medium effort - requires moderate configuration or code changes.</summary>
    Medium,
    /// <summary>High effort - requires significant changes or additional resources.</summary>
    High
}

/// <summary>
/// Defines the severity levels for performance bottlenecks.
/// </summary>
public enum BottleneckSeverity
{
    /// <summary>Low severity - minor impact on performance.</summary>
    Low,
    /// <summary>Medium severity - noticeable impact on performance.</summary>
    Medium,
    /// <summary>High severity - significant impact on performance.</summary>
    High,
    /// <summary>Critical severity - severe impact on performance requiring immediate attention.</summary>
    Critical
}
