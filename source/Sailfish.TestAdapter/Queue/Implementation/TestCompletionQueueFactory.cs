using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Implementation;

/// <summary>
/// Factory implementation for creating and configuring test completion queue instances
/// in the intercepting queue architecture. This factory provides centralized queue creation,
/// configuration validation, and lifecycle management to support the in-memory queue system
/// used for batch processing and cross-test-case analysis.
/// </summary>
/// <remarks>
/// The TestCompletionQueueFactory is a key component of the queue infrastructure that
/// abstracts the creation and configuration of queue instances. This implementation
/// currently supports in-memory queue creation using the InMemoryTestCompletionQueue
/// but is designed to be extensible for future queue implementations.
/// 
/// Key responsibilities:
/// - Create properly configured queue instances based on provided configuration
/// - Validate queue configuration parameters before instantiation
/// - Provide comprehensive error handling for queue creation failures
/// - Support both custom and default configuration scenarios
/// - Integrate with the Sailfish logging infrastructure for diagnostics
/// - Ensure thread-safe operations for concurrent queue creation
/// 
/// The factory follows the factory pattern to decouple queue creation from queue usage,
/// enabling configuration-driven queue instantiation and supporting dependency injection
/// scenarios. The factory is designed to be registered as a singleton service in the
/// DI container and can handle concurrent queue creation requests safely.
/// 
/// Thread Safety:
/// This implementation is thread-safe and can handle concurrent queue creation requests
/// from multiple test execution contexts. The factory does not maintain shared mutable
/// state between method calls, ensuring safe concurrent access.
/// 
/// Integration:
/// The factory integrates with the existing Sailfish logging infrastructure and
/// configuration system. It works closely with the queue manager and other queue
/// infrastructure components to provide properly configured queue instances for
/// the intercepting queue architecture.
/// </remarks>
public class TestCompletionQueueFactory : ITestCompletionQueueFactory
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCompletionQueueFactory"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger service for recording factory operations, queue creation events,
    /// and diagnostic information. Used for troubleshooting queue creation issues
    /// and monitoring factory usage.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// The factory requires a logger for comprehensive diagnostic information and
    /// error reporting. This dependency is typically injected by the DI container
    /// during factory registration and instantiation.
    /// </remarks>
    public TestCompletionQueueFactory(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ITestCompletionQueue> CreateQueueAsync(QueueConfiguration configuration, CancellationToken cancellationToken)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        cancellationToken.ThrowIfCancellationRequested();

        _logger.Log(LogLevel.Debug, "Starting queue creation with custom configuration");

        // Validate configuration before creating queue
        var validationErrors = await GetValidationErrorsAsync(configuration).ConfigureAwait(false);
        if (validationErrors.Length > 0)
        {
            var errorMessage = $"Queue configuration validation failed: {string.Join("; ", validationErrors)}";
            _logger.Log(LogLevel.Error, errorMessage);
            throw new ArgumentException(errorMessage, nameof(configuration));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Create the in-memory queue instance with the configured capacity
            var queue = new InMemoryTestCompletionQueue(configuration.MaxQueueCapacity);

            _logger.Log(LogLevel.Information,
                "Successfully created in-memory test completion queue with capacity: {0}, " +
                "batch processing enabled: {1}, max batch size: {2}",
                configuration.MaxQueueCapacity, configuration.EnableBatchProcessing, configuration.MaxBatchSize);

            return queue;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to create test completion queue: {ex.Message}";
            _logger.Log(LogLevel.Error, ex, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    /// <inheritdoc />
    public async Task<ITestCompletionQueue> CreateQueueAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.Log(LogLevel.Debug, "Starting queue creation with default configuration");

        // Create default configuration optimized for typical test execution scenarios
        var defaultConfiguration = new QueueConfiguration
        {
            IsEnabled = true,
            MaxQueueCapacity = 1000,
            PublishTimeoutMs = 5000,
            ProcessingTimeoutMs = 30000,
            BatchCompletionTimeoutMs = 60000,
            MaxRetryAttempts = 3,
            BaseRetryDelayMs = 1000,
            EnableBatchProcessing = true,
            MaxBatchSize = 50,
            EnableFrameworkPublishing = true,
            EnableLoggingProcessor = false,
            EnableComparisonAnalysis = false,
            EnableFallbackPublishing = true,
            LogLevel = LogLevel.Information
        };

        _logger.Log(LogLevel.Information, "Using default queue configuration for queue creation");

        // Delegate to the main creation method
        return await CreateQueueAsync(defaultConfiguration, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> ValidateConfigurationAsync(QueueConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _logger.Log(LogLevel.Debug, "Validating queue configuration");

        // Use the configuration's built-in validation method
        var validationErrors = configuration.Validate();
        var isValid = validationErrors.Length == 0;

        _logger.Log(LogLevel.Debug,
            "Queue configuration validation completed. Valid: {0}, Error count: {1}",
            isValid, validationErrors.Length);

        return Task.FromResult(isValid);
    }

    /// <inheritdoc />
    public Task<string[]> GetValidationErrorsAsync(QueueConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _logger.Log(LogLevel.Debug, "Getting validation errors for queue configuration");

        // Use the configuration's built-in validation method
        var validationErrors = configuration.Validate();

        if (validationErrors.Length > 0)
        {
            _logger.Log(LogLevel.Warning,
                "Queue configuration validation found {0} errors: {1}",
                validationErrors.Length, string.Join("; ", validationErrors));
        }
        else
        {
            _logger.Log(LogLevel.Debug, "Queue configuration validation passed with no errors");
        }

        return Task.FromResult(validationErrors);
    }

    /// <inheritdoc />
    public Task<string[]> GetSupportedQueueTypesAsync()
    {
        _logger.Log(LogLevel.Debug, "Getting supported queue types information");

        var supportedTypes = new[]
        {
            "InMemory - High-performance in-memory queue using System.Threading.Channels. " +
            "Provides thread-safe operations within the test adapter process lifetime. " +
            "Optimized for test execution scenarios with no persistence requirements. " +
            "Supports configurable bounded capacity for controlled memory usage. " +
            "Includes proper lifecycle management and graceful shutdown capabilities."
        };

        _logger.Log(LogLevel.Debug, "Returning information for {0} supported queue types", supportedTypes.Length);

        return Task.FromResult(supportedTypes);
    }
}
