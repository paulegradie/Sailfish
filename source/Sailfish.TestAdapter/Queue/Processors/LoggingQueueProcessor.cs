using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Processors;

/// <summary>
/// A sample queue processor that logs test completion events with configurable detail levels.
/// This processor serves as an example implementation of the <see cref="TestCompletionQueueProcessorBase"/>
/// and demonstrates best practices for creating custom queue processors in the intercepting queue architecture.
/// </summary>
/// <remarks>
/// The LoggingQueueProcessor is designed to provide comprehensive logging of test completion events
/// for debugging, monitoring, and analysis purposes. It supports configurable logging levels and
/// can selectively log different aspects of test execution based on configuration settings.
/// 
/// Key Features:
/// - Configurable minimum log level for processor output
/// - Optional detailed performance metrics logging
/// - Optional metadata logging for test context information
/// - Structured exception logging for failed tests
/// - Thread-safe operation for concurrent test execution scenarios
/// - Integration with existing Sailfish logging infrastructure
/// 
/// Usage Scenarios:
/// - Development and debugging: Enable verbose logging to see detailed test execution information
/// - Production monitoring: Use information level logging to track test completion status
/// - Performance analysis: Enable performance metrics logging to analyze test execution patterns
/// - Troubleshooting: Enable full logging to diagnose test execution issues
/// 
/// Configuration:
/// The processor accepts a <see cref="LoggingProcessorConfiguration"/> object that controls
/// what information is logged and at what level. This allows fine-tuning of log output
/// based on specific requirements and environments.
/// 
/// Thread Safety:
/// This processor is thread-safe and can handle concurrent test completion events.
/// The thread safety is achieved through stateless design and delegation to the
/// thread-safe base class and logger implementations.
/// </remarks>
public class LoggingQueueProcessor : TestCompletionQueueProcessorBase
{
    private readonly LoggingProcessorConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingQueueProcessor"/> class with default configuration.
    /// </summary>
    /// <param name="logger">
    /// The logger service for recording processor operations and test completion information.
    /// This logger is used for all processor output and integrates with the Sailfish logging infrastructure.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// This constructor creates a logging processor with default configuration settings:
    /// - Minimum log level: Information
    /// - Performance metrics logging: Enabled
    /// - Metadata logging: Disabled (to reduce verbosity)
    /// - Full exception details: Enabled
    /// 
    /// For custom configuration, use the overloaded constructor that accepts a configuration object.
    /// </remarks>
    public LoggingQueueProcessor(ILogger logger) 
        : this(logger, new LoggingProcessorConfiguration())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingQueueProcessor"/> class with custom configuration.
    /// </summary>
    /// <param name="logger">
    /// The logger service for recording processor operations and test completion information.
    /// This logger is used for all processor output and integrates with the Sailfish logging infrastructure.
    /// </param>
    /// <param name="configuration">
    /// The configuration object that controls logging behavior, including log levels,
    /// performance metrics logging, metadata logging, and exception detail levels.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> or <paramref name="configuration"/> is null.
    /// </exception>
    /// <remarks>
    /// This constructor allows full customization of the logging processor behavior through
    /// the configuration object. The configuration controls what information is logged
    /// and at what level, enabling fine-tuning for different environments and use cases.
    /// </remarks>
    public LoggingQueueProcessor(ILogger logger, LoggingProcessorConfiguration configuration) 
        : base(logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    protected override async Task ProcessTestCompletionCore(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        // Ensure we respect cancellation requests
        cancellationToken.ThrowIfCancellationRequested();

        // Log basic test completion information
        await LogTestCompletion(message, cancellationToken).ConfigureAwait(false);

        // Log performance metrics if enabled and available
        if (_configuration.LogPerformanceMetrics && message.PerformanceMetrics != null)
        {
            await LogPerformanceMetrics(message, cancellationToken).ConfigureAwait(false);
        }

        // Log metadata if enabled and available
        if (_configuration.LogMetadata && message.Metadata?.Count > 0)
        {
            await LogMetadata(message, cancellationToken).ConfigureAwait(false);
        }

        // Log exception details for failed tests
        if (!message.TestResult.IsSuccess && message.TestResult.ExceptionMessage != null)
        {
            await LogExceptionDetails(message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Logs basic test completion information including test case ID, success status, and completion time.
    /// </summary>
    /// <param name="message">The test completion message containing test execution data.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous logging operation.</returns>
    private Task LogTestCompletion(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var status = message.TestResult.IsSuccess ? "PASSED" : "FAILED";
        var logLevel = message.TestResult.IsSuccess ? LogLevel.Information : LogLevel.Warning;

        Logger.Log(logLevel, 
            "Test '{TestCaseId}' completed with status {Status} at {CompletedAt:yyyy-MM-dd HH:mm:ss.fff}",
            message.TestCaseId, status, message.CompletedAt);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs detailed performance metrics for the test execution.
    /// </summary>
    /// <param name="message">The test completion message containing performance metrics.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous logging operation.</returns>
    private Task LogPerformanceMetrics(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metrics = message.PerformanceMetrics;
        var metricsBuilder = new StringBuilder();
        
        metricsBuilder.AppendLine($"Performance metrics for test '{message.TestCaseId}':");
        metricsBuilder.AppendLine($"  Mean: {metrics.MeanMs:F3} ms");
        metricsBuilder.AppendLine($"  Median: {metrics.MedianMs:F3} ms");
        metricsBuilder.AppendLine($"  Standard Deviation: {metrics.StandardDeviation:F3}");
        metricsBuilder.AppendLine($"  Sample Size: {metrics.SampleSize}");
        metricsBuilder.AppendLine($"  Warmup Iterations: {metrics.NumWarmupIterations}");
        
        if (metrics.TotalNumOutliers > 0)
        {
            metricsBuilder.AppendLine($"  Outliers Detected: {metrics.TotalNumOutliers}");
        }

        if (!string.IsNullOrEmpty(metrics.GroupingId))
        {
            metricsBuilder.AppendLine($"  Grouping ID: {metrics.GroupingId}");
        }

        Logger.Log(LogLevel.Debug, metricsBuilder.ToString().TrimEnd());

        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs metadata information associated with the test execution.
    /// </summary>
    /// <param name="message">The test completion message containing metadata.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous logging operation.</returns>
    private Task LogMetadata(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metadataBuilder = new StringBuilder();
        metadataBuilder.AppendLine($"Metadata for test '{message.TestCaseId}':");

        foreach (var kvp in message.Metadata.OrderBy(x => x.Key))
        {
            var value = kvp.Value?.ToString() ?? "<null>";
            metadataBuilder.AppendLine($"  {kvp.Key}: {value}");
        }

        Logger.Log(LogLevel.Verbose, metadataBuilder.ToString().TrimEnd());

        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs detailed exception information for failed tests.
    /// </summary>
    /// <param name="message">The test completion message containing exception details.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous logging operation.</returns>
    private Task LogExceptionDetails(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exceptionBuilder = new StringBuilder();
        exceptionBuilder.AppendLine($"Exception details for failed test '{message.TestCaseId}':");
        
        if (!string.IsNullOrEmpty(message.TestResult.ExceptionType))
        {
            exceptionBuilder.AppendLine($"  Exception Type: {message.TestResult.ExceptionType}");
        }
        
        exceptionBuilder.AppendLine($"  Message: {message.TestResult.ExceptionMessage}");
        
        if (_configuration.LogFullExceptionDetails && !string.IsNullOrEmpty(message.TestResult.ExceptionDetails))
        {
            exceptionBuilder.AppendLine($"  Details: {message.TestResult.ExceptionDetails}");
        }

        Logger.Log(LogLevel.Error, exceptionBuilder.ToString().TrimEnd());

        return Task.CompletedTask;
    }
}

/// <summary>
/// Configuration settings for the <see cref="LoggingQueueProcessor"/> that control
/// what information is logged and at what level of detail.
/// </summary>
/// <remarks>
/// This configuration class allows fine-tuning of the logging processor behavior
/// to match different environments and use cases. For example:
///
/// - Development: Enable all logging options for maximum visibility
/// - Production: Disable verbose options to reduce log volume
/// - Performance Analysis: Enable only performance metrics logging
/// - Troubleshooting: Enable full exception details and metadata
///
/// The configuration is immutable after construction to ensure thread safety
/// and consistent behavior across concurrent test execution scenarios.
/// </remarks>
public class LoggingProcessorConfiguration
{
    /// <summary>
    /// Gets a value indicating whether to log detailed performance metrics.
    /// When enabled, logs execution times, statistical data, and outlier information at Debug level.
    /// </summary>
    /// <value>
    /// <c>true</c> to log performance metrics; otherwise, <c>false</c>.
    /// Default value is <c>true</c>.
    /// </value>
    public bool LogPerformanceMetrics { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to log metadata associated with test execution.
    /// When enabled, logs all metadata key-value pairs at Verbose level.
    /// </summary>
    /// <value>
    /// <c>true</c> to log metadata; otherwise, <c>false</c>.
    /// Default value is <c>false</c> to reduce log verbosity.
    /// </value>
    public bool LogMetadata { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to log full exception details for failed tests.
    /// When enabled, includes stack traces and detailed exception information.
    /// When disabled, only logs exception type and message.
    /// </summary>
    /// <value>
    /// <c>true</c> to log full exception details; otherwise, <c>false</c>.
    /// Default value is <c>true</c> for comprehensive error information.
    /// </value>
    public bool LogFullExceptionDetails { get; init; } = true;

    /// <summary>
    /// Gets the minimum log level for processor output.
    /// Messages below this level will not be logged by the processor.
    /// </summary>
    /// <value>
    /// The minimum log level. Default value is <see cref="LogLevel.Information"/>.
    /// </value>
    /// <remarks>
    /// This setting controls the overall verbosity of the processor output.
    /// Note that individual log statements may use different levels based on
    /// the type of information being logged:
    /// - Information: Basic test completion status
    /// - Debug: Performance metrics
    /// - Verbose: Metadata information
    /// - Warning: Failed test notifications
    /// - Error: Exception details
    /// </remarks>
    public LogLevel MinimumLogLevel { get; init; } = LogLevel.Information;

    /// <summary>
    /// Creates a new configuration instance with development-friendly settings.
    /// Enables all logging options for maximum visibility during development and debugging.
    /// </summary>
    /// <returns>
    /// A configuration instance with all logging options enabled and minimum log level set to Verbose.
    /// </returns>
    public static LoggingProcessorConfiguration CreateDevelopmentConfiguration()
    {
        return new LoggingProcessorConfiguration
        {
            LogPerformanceMetrics = true,
            LogMetadata = true,
            LogFullExceptionDetails = true,
            MinimumLogLevel = LogLevel.Verbose
        };
    }

    /// <summary>
    /// Creates a new configuration instance with production-friendly settings.
    /// Disables verbose logging options to reduce log volume in production environments.
    /// </summary>
    /// <returns>
    /// A configuration instance with minimal logging options and minimum log level set to Information.
    /// </returns>
    public static LoggingProcessorConfiguration CreateProductionConfiguration()
    {
        return new LoggingProcessorConfiguration
        {
            LogPerformanceMetrics = false,
            LogMetadata = false,
            LogFullExceptionDetails = false,
            MinimumLogLevel = LogLevel.Information
        };
    }

    /// <summary>
    /// Creates a new configuration instance optimized for performance analysis.
    /// Enables performance metrics logging while disabling other verbose options.
    /// </summary>
    /// <returns>
    /// A configuration instance with performance metrics enabled and other options disabled.
    /// </returns>
    public static LoggingProcessorConfiguration CreatePerformanceAnalysisConfiguration()
    {
        return new LoggingProcessorConfiguration
        {
            LogPerformanceMetrics = true,
            LogMetadata = false,
            LogFullExceptionDetails = false,
            MinimumLogLevel = LogLevel.Debug
        };
    }
}
