using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Sailfish.Logging;

namespace Sailfish.TestAdapter.Queue.Configuration;

/// <summary>
/// Configuration model for the in-memory queue system that controls queue behavior,
/// processor settings, and operational parameters. This configuration enables fine-tuning
/// of the intercepting queue architecture for optimal performance in different testing scenarios.
/// </summary>
/// <remarks>
/// The QueueConfiguration class provides comprehensive settings for controlling all aspects
/// of the in-memory queue system, including:
/// 
/// - Queue capacity and memory management settings
/// - Processor enablement and execution parameters
/// - Timeout and retry configurations for robust operation
/// - Batching and grouping settings for cross-test-case analysis
/// - Fallback and error handling configurations
/// 
/// All settings are designed with sensible defaults optimized for in-memory operations
/// within the test adapter runtime. The configuration supports both high-throughput
/// scenarios with many concurrent test cases and smaller test suites with minimal overhead.
/// 
/// Thread Safety:
/// This configuration class is designed to be thread-safe for read operations after
/// initialization. Configuration changes should be coordinated through the configuration
/// management system to ensure consistency across all queue components.
/// 
/// Integration:
/// This configuration integrates with the existing Sailfish settings system and can be
/// loaded from run settings, configuration files, or programmatically configured.
/// The settings are used by queue managers, processors, and other queue infrastructure
/// components to control their behavior and resource usage.
/// </remarks>
public class QueueConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the queue system is enabled.
    /// When disabled, the test adapter falls back to direct framework publishing.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable queue processing; <c>false</c> to use direct framework publishing.
    /// Default is <c>false</c> to ensure backward compatibility.
    /// </value>
    /// <remarks>
    /// This is the master switch for the entire queue system. When disabled, test completion
    /// notifications will bypass the queue and be published directly to the framework,
    /// maintaining the original behavior. This provides a safe fallback mechanism and
    /// allows gradual adoption of queue-based processing.
    /// </remarks>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum capacity of the in-memory queue.
    /// This limits the number of messages that can be queued simultaneously.
    /// </summary>
    /// <value>
    /// The maximum number of messages the queue can hold. Default is 1000.
    /// </value>
    /// <remarks>
    /// This setting prevents memory issues during high-throughput test execution by
    /// limiting the queue size. When the queue reaches capacity, publishing operations
    /// may block or fail depending on the queue implementation. The default value is
    /// optimized for typical test suites while preventing excessive memory usage.
    /// 
    /// Consider increasing this value for large test suites with many concurrent test cases,
    /// or decreasing it for memory-constrained environments.
    /// </remarks>
    [JsonPropertyName("maxQueueCapacity")]
    public int MaxQueueCapacity { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the timeout for queue publishing operations in milliseconds.
    /// </summary>
    /// <value>
    /// The timeout in milliseconds for queue publishing operations. Default is 5000 (5 seconds).
    /// </value>
    /// <remarks>
    /// This timeout prevents queue publishing operations from hanging indefinitely.
    /// If a publishing operation takes longer than this timeout, it will be cancelled
    /// and may trigger fallback mechanisms. The default value provides a reasonable
    /// balance between allowing for temporary queue congestion and preventing hangs.
    /// </remarks>
    [JsonPropertyName("publishTimeoutMs")]
    public int PublishTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the timeout for queue processing operations in milliseconds.
    /// </summary>
    /// <value>
    /// The timeout in milliseconds for individual processor execution. Default is 30000 (30 seconds).
    /// </value>
    /// <remarks>
    /// This timeout controls how long individual queue processors can run before being
    /// cancelled. This prevents slow or hanging processors from blocking the entire
    /// queue processing pipeline. Processors that exceed this timeout will be cancelled
    /// and may trigger retry or fallback mechanisms.
    /// </remarks>
    [JsonPropertyName("processingTimeoutMs")]
    public int ProcessingTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the timeout for batch completion detection in milliseconds.
    /// </summary>
    /// <value>
    /// The timeout in milliseconds for waiting for batch completion. Default is 60000 (60 seconds).
    /// </value>
    /// <remarks>
    /// This timeout determines how long the system will wait for all test cases in a batch
    /// to complete before processing incomplete batches. This is crucial for cross-test-case
    /// analysis where the system needs to wait for related test cases to finish execution.
    /// 
    /// If a batch doesn't complete within this timeout, the available test cases will be
    /// processed and the missing ones will be handled when they eventually complete.
    /// </remarks>
    [JsonPropertyName("batchCompletionTimeoutMs")]
    public int BatchCompletionTimeoutMs { get; set; } = 60000;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed queue operations.
    /// </summary>
    /// <value>
    /// The maximum number of retry attempts. Default is 3.
    /// </value>
    /// <remarks>
    /// This setting controls how many times failed queue operations (publishing, processing)
    /// will be retried before giving up. Retries use exponential backoff to avoid
    /// overwhelming the system. After all retries are exhausted, fallback mechanisms
    /// may be triggered to ensure test results are not lost.
    /// </remarks>
    [JsonPropertyName("maxRetryAttempts")]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for retry operations in milliseconds.
    /// </summary>
    /// <value>
    /// The base delay in milliseconds between retry attempts. Default is 1000 (1 second).
    /// </value>
    /// <remarks>
    /// This is the initial delay used for exponential backoff retry logic. Each subsequent
    /// retry will wait longer (typically doubling the delay) to avoid overwhelming a
    /// system that may be temporarily overloaded. The actual delay for retry attempt N
    /// will be approximately: BaseRetryDelayMs * (2 ^ (N-1)).
    /// </remarks>
    [JsonPropertyName("baseRetryDelayMs")]
    public int BaseRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a value indicating whether batch processing is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable batch processing; <c>false</c> to process test cases individually.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// Batch processing enables cross-test-case analysis by grouping related test cases
    /// and processing them together. This is essential for performance comparison and
    /// statistical analysis across multiple test methods. When disabled, test cases
    /// are processed individually without cross-test-case analysis capabilities.
    /// </remarks>
    [JsonPropertyName("enableBatchProcessing")]
    public bool EnableBatchProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum batch size for grouping test cases.
    /// </summary>
    /// <value>
    /// The maximum number of test cases that can be grouped in a single batch. Default is 50.
    /// </value>
    /// <remarks>
    /// This setting limits the size of test case batches to prevent excessive memory usage
    /// and processing delays. Larger batches enable more comprehensive cross-test-case
    /// analysis but require more memory and processing time. Smaller batches process
    /// faster but may limit the scope of analysis.
    /// 
    /// The optimal batch size depends on the complexity of your test cases and the
    /// available system resources.
    /// </remarks>
    [JsonPropertyName("maxBatchSize")]
    public int MaxBatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets a value indicating whether the framework publishing processor is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable framework publishing; <c>false</c> to disable.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// The framework publishing processor is responsible for sending test results to the
    /// VS Test Platform. This should normally be enabled unless you have custom processors
    /// that handle framework publishing. Disabling this will prevent test results from
    /// appearing in test explorers and may cause tests to appear as hanging.
    /// </remarks>
    [JsonPropertyName("enableFrameworkPublishing")]
    public bool EnableFrameworkPublishing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether logging processor is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable logging of test completion events; <c>false</c> to disable.
    /// Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// The logging processor provides detailed logging of test completion events for
    /// debugging and monitoring purposes. Enable this for troubleshooting queue issues
    /// or when detailed test execution logging is required. This may impact performance
    /// in high-throughput scenarios.
    /// </remarks>
    [JsonPropertyName("enableLoggingProcessor")]
    public bool EnableLoggingProcessor { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether comparison analysis processor is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable cross-test-case comparison analysis; <c>false</c> to disable.
    /// Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// The comparison analysis processor performs cross-test-case performance comparisons
    /// and statistical analysis. This enables advanced features like performance ranking,
    /// regression detection, and baseline comparisons. Enable this for comprehensive
    /// performance analysis across test batches.
    /// </remarks>
    [JsonPropertyName("enableComparisonAnalysis")]
    public bool EnableComparisonAnalysis { get; set; } = false;

    /// <summary>
    /// Gets or sets whether method comparison processing is enabled.
    /// When enabled, methods marked with SailfishComparisonAttribute will be
    /// grouped and compared when full test classes are executed.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable method comparison processing; <c>false</c> to disable.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// This setting enables the method comparison feature that performs SailDiff analysis
    /// between methods marked with SailfishComparisonAttribute. When enabled, methods with
    /// the same comparison group will be compared against each other when the full test
    /// class is executed. Individual method execution will not trigger comparisons.
    /// </remarks>
    [JsonPropertyName("enableMethodComparison")]
    public bool EnableMethodComparison { get; set; } = true;

    /// <summary>
    /// Gets or sets the strategy for detecting full class execution vs individual method execution.
    /// </summary>
    /// <value>
    /// The strategy used to determine when to perform method comparisons.
    /// Default is <c>ComparisonDetectionStrategy.ByTestCaseCount</c>.
    /// </value>
    /// <remarks>
    /// This setting controls when method comparisons are performed:
    /// - ByTestCaseCount: Compare executed methods vs all SailfishMethods in class
    /// - Always: Perform comparisons when comparison groups are complete
    /// - Never: Disable method comparison processing
    /// </remarks>
    [JsonPropertyName("comparisonDetectionStrategy")]
    public ComparisonDetectionStrategy ComparisonDetectionStrategy { get; set; } = ComparisonDetectionStrategy.ByTestCaseCount;

    /// <summary>
    /// Gets or sets the timeout for comparison processing in milliseconds.
    /// </summary>
    /// <value>
    /// The timeout in milliseconds for method comparison operations. Default is 30000 (30 seconds).
    /// </value>
    /// <remarks>
    /// This timeout prevents comparison processing from hanging indefinitely when performing
    /// SailDiff analysis between methods. If comparison processing takes longer than this
    /// timeout, it will be cancelled and the test will continue with normal results.
    /// </remarks>
    [JsonPropertyName("comparisonTimeoutMs")]
    public int ComparisonTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets a value indicating whether fallback to direct framework publishing is enabled
    /// when queue processing fails.
    /// </summary>
    /// <value>
    /// <c>true</c> to enable fallback to direct publishing; <c>false</c> to disable.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// This setting enables automatic fallback to direct framework publishing when queue
    /// processing fails or times out. This ensures that test results are never lost due
    /// to queue issues. Disabling fallback may result in lost test results if queue
    /// processing fails, but can be useful for debugging queue issues.
    /// 
    /// It is strongly recommended to keep this enabled in production environments.
    /// </remarks>
    [JsonPropertyName("enableFallbackPublishing")]
    public bool EnableFallbackPublishing { get; set; } = true;

    /// <summary>
    /// Gets or sets the log level for queue operations.
    /// </summary>
    /// <value>
    /// The log level for queue-related logging. Default is LogLevel.Information.
    /// </value>
    /// <remarks>
    /// This setting controls the verbosity of queue-related logging. Valid values include:
    /// - LogLevel.Verbose: Very detailed logging for debugging
    /// - LogLevel.Debug: Detailed logging for development
    /// - LogLevel.Information: General operational information
    /// - LogLevel.Warning: Warning messages only
    /// - LogLevel.Error: Error messages only
    /// - LogLevel.Fatal: Critical errors only
    ///
    /// Higher verbosity levels may impact performance in high-throughput scenarios.
    /// </remarks>
    [JsonPropertyName("logLevel")]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Validates the configuration settings and returns any validation errors.
    /// </summary>
    /// <returns>
    /// An array of validation error messages. Empty array if configuration is valid.
    /// </returns>
    /// <remarks>
    /// This method performs comprehensive validation of all configuration settings to ensure
    /// they are within acceptable ranges and compatible with each other. It should be called
    /// after loading configuration to ensure the queue system will operate correctly.
    /// 
    /// Validation includes:
    /// - Positive values for timeouts and capacities
    /// - Reasonable ranges for retry settings
    /// - Valid log level values
    /// - Logical consistency between related settings
    /// </remarks>
    public string[] Validate()
    {
        var errors = new List<string>();

        if (MaxQueueCapacity <= 0)
            errors.Add("MaxQueueCapacity must be greater than 0");

        if (PublishTimeoutMs <= 0)
            errors.Add("PublishTimeoutMs must be greater than 0");

        if (ProcessingTimeoutMs <= 0)
            errors.Add("ProcessingTimeoutMs must be greater than 0");

        if (BatchCompletionTimeoutMs <= 0)
            errors.Add("BatchCompletionTimeoutMs must be greater than 0");

        if (MaxRetryAttempts < 0)
            errors.Add("MaxRetryAttempts must be greater than or equal to 0");

        if (BaseRetryDelayMs <= 0)
            errors.Add("BaseRetryDelayMs must be greater than 0");

        if (MaxBatchSize <= 0)
            errors.Add("MaxBatchSize must be greater than 0");

        if (ComparisonTimeoutMs <= 0)
            errors.Add("ComparisonTimeoutMs must be greater than 0");

        if (!Enum.IsDefined(typeof(LogLevel), LogLevel))
            errors.Add($"LogLevel must be a valid LogLevel enum value");

        if (EnableBatchProcessing && MaxBatchSize > MaxQueueCapacity)
            errors.Add("MaxBatchSize cannot be greater than MaxQueueCapacity when batch processing is enabled");

        return errors.ToArray();
    }
}
