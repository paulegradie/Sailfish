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
}
