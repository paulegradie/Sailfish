using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.TestAdapter.Queue.Configuration;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for creating and managing test completion queue instances
/// in the intercepting queue architecture. This factory interface provides a centralized
/// mechanism for queue creation, configuration validation, and lifecycle management
/// to support different queue implementations and deployment scenarios.
/// </summary>
/// <remarks>
/// The ITestCompletionQueueFactory is a key component of the queue infrastructure
/// that abstracts the creation and configuration of queue instances. This factory
/// pattern enables:
/// 
/// - Configuration-driven queue creation based on runtime settings
/// - Support for multiple queue implementations (currently in-memory only)
/// - Centralized validation of queue configuration before instantiation
/// - Proper lifecycle management and resource initialization
/// - Thread-safe queue creation for concurrent execution scenarios
/// - Integration with the dependency injection container
/// 
/// The factory is responsible for:
/// - Validating queue configuration parameters before creation
/// - Creating appropriate queue instances based on configuration
/// - Initializing queue instances with proper settings
/// - Providing error handling for queue creation failures
/// - Supporting future extensibility for additional queue types
/// 
/// Thread Safety:
/// Implementations of this interface must be thread-safe to support concurrent
/// queue creation scenarios during test execution. Multiple test execution contexts
/// may request queue instances simultaneously.
/// 
/// Integration:
/// The factory integrates with the Sailfish dependency injection system and is
/// typically registered as a singleton service. It works closely with the queue
/// manager and configuration system to provide properly configured queue instances.
/// </remarks>
public interface ITestCompletionQueueFactory
{
    /// <summary>
    /// Creates a new test completion queue instance based on the provided configuration.
    /// </summary>
    /// <param name="configuration">
    /// The queue configuration containing all settings required for queue creation,
    /// including capacity limits, timeout values, and operational parameters.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the queue creation operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous queue creation operation.
    /// The task result contains the configured and initialized queue instance
    /// ready for use in the test execution pipeline.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the configuration contains invalid settings that prevent
    /// queue creation. The exception message will contain details about the
    /// specific validation failures.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when queue creation fails due to system constraints or resource
    /// limitations that prevent successful queue initialization.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method performs comprehensive validation of the provided configuration
    /// before attempting to create the queue instance. If validation fails, an
    /// ArgumentException will be thrown with details about the specific issues.
    /// 
    /// The created queue instance will be properly initialized and ready for use,
    /// but will not be started automatically. The caller is responsible for calling
    /// StartAsync on the returned queue instance when ready to begin processing.
    /// 
    /// The factory supports creating multiple queue instances with different
    /// configurations, though typically only one queue instance is used per
    /// test execution context.
    /// </remarks>
    Task<ITestCompletionQueue> CreateQueueAsync(QueueConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new test completion queue instance using default configuration settings.
    /// This is a convenience method for scenarios where custom configuration is not required.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the queue creation operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous queue creation operation.
    /// The task result contains the queue instance configured with default settings
    /// and ready for use in the test execution pipeline.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when queue creation fails due to system constraints or resource
    /// limitations that prevent successful queue initialization.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method creates a queue instance using a default QueueConfiguration with
    /// settings optimized for typical test execution scenarios. The default configuration
    /// includes reasonable capacity limits, timeout values, and operational parameters
    /// suitable for most use cases.
    /// 
    /// For scenarios requiring custom configuration (such as high-throughput test suites
    /// or memory-constrained environments), use the CreateQueueAsync overload that
    /// accepts a QueueConfiguration parameter.
    /// </remarks>
    Task<ITestCompletionQueue> CreateQueueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Validates the provided queue configuration without creating a queue instance.
    /// This method can be used to verify configuration validity before queue creation.
    /// </summary>
    /// <param name="configuration">
    /// The queue configuration to validate for correctness and compatibility.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous validation operation.
    /// The task result is true if the configuration is valid; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> is null.
    /// </exception>
    /// <remarks>
    /// This method performs the same validation logic used during queue creation
    /// but without the overhead of actually creating a queue instance. It can be
    /// useful for configuration validation during application startup or for
    /// providing early feedback about configuration issues.
    /// 
    /// If validation fails, the method returns false rather than throwing an
    /// exception. For detailed validation error information, use the
    /// GetValidationErrorsAsync method.
    /// </remarks>
    Task<bool> ValidateConfigurationAsync(QueueConfiguration configuration);

    /// <summary>
    /// Gets detailed validation error information for the provided queue configuration.
    /// </summary>
    /// <param name="configuration">
    /// The queue configuration to validate and analyze for errors.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous validation operation.
    /// The task result contains an array of validation error messages.
    /// An empty array indicates the configuration is valid.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> is null.
    /// </exception>
    /// <remarks>
    /// This method provides detailed diagnostic information about configuration
    /// validation failures. Each error message describes a specific issue with
    /// the configuration that would prevent successful queue creation.
    /// 
    /// The validation covers:
    /// - Required property values and ranges
    /// - Logical consistency between related settings
    /// - Resource constraints and system limitations
    /// - Compatibility with the current runtime environment
    /// 
    /// This method is particularly useful for configuration debugging and
    /// providing user-friendly error messages in configuration management scenarios.
    /// </remarks>
    Task<string[]> GetValidationErrorsAsync(QueueConfiguration configuration);

    /// <summary>
    /// Gets information about the supported queue types and their capabilities.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains an array of strings describing the supported
    /// queue implementations and their key characteristics.
    /// </returns>
    /// <remarks>
    /// This method provides metadata about the queue implementations supported
    /// by the factory. Currently, this includes information about the in-memory
    /// queue implementation, but the interface is designed to support future
    /// queue types such as persistent or distributed queues.
    /// 
    /// The returned information can be used for:
    /// - Configuration documentation and help systems
    /// - Runtime capability detection
    /// - Diagnostic and troubleshooting scenarios
    /// - Future extensibility planning
    /// </remarks>
    Task<string[]> GetSupportedQueueTypesAsync();
}
