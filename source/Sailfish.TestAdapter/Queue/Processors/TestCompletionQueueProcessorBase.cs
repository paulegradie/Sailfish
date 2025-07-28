using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Processors;

/// <summary>
/// Abstract base class for test completion queue processors that provides common functionality
/// for error handling, logging, and template method pattern for processing logic.
/// This class is part of the intercepting queue architecture that enables asynchronous processing,
/// batch analysis, and enhanced result generation before test results are reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// The TestCompletionQueueProcessorBase implements the template method pattern to provide
/// a consistent processing framework for all queue processors while allowing derived classes
/// to implement specific processing logic. The base class handles common concerns such as:
/// 
/// - Parameter validation and null checking
/// - Comprehensive error handling and logging
/// - Cancellation token propagation and handling
/// - Processing lifecycle hooks for extensibility
/// - Thread-safe operations for concurrent execution scenarios
/// 
/// Derived classes must implement the ProcessTestCompletionCore method to provide their
/// specific processing logic. Optional virtual methods are available for customizing
/// pre-processing, post-processing, and error handling behavior.
/// 
/// Common processor implementations that extend this base class include:
/// - Framework Publishing Processor: Publishes FrameworkTestCaseEndNotification to VS Test Platform
/// - Test Case Comparison Processor: Performs cross-test-case performance comparisons
/// - Batch Completion Processor: Detects when test case batches are complete
/// - Historical Data Processor: Stores test results for trend analysis
/// - Report Generation Processor: Creates automated test reports
/// - Alerting Processor: Sends notifications for test failures or performance issues
/// 
/// Thread Safety:
/// This base class is designed to be thread-safe and can handle concurrent execution
/// scenarios where multiple test cases complete simultaneously. The thread safety is
/// achieved through stateless design and delegation to thread-safe dependencies.
/// Derived classes are responsible for ensuring their own thread safety.
/// </remarks>
public abstract class TestCompletionQueueProcessorBase : ITestCompletionQueueProcessor
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCompletionQueueProcessorBase"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger service for recording processor operations, errors, and diagnostic information.
    /// Used to log processing lifecycle events, errors, and other significant events for troubleshooting.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// The base processor requires a logger for comprehensive error reporting and diagnostic
    /// information. This dependency is typically injected by the DI container during
    /// processor registration and instantiation.
    /// </remarks>
    protected TestCompletionQueueProcessorBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the logger instance for use by derived classes.
    /// </summary>
    /// <value>
    /// The logger service for recording processor-specific operations and diagnostic information.
    /// </value>
    /// <remarks>
    /// Derived classes can use this logger to record their specific processing events,
    /// errors, and diagnostic information. The logger follows the existing Sailfish
    /// logging patterns and integrates with the test adapter's logging infrastructure.
    /// </remarks>
    protected ILogger Logger => _logger;

    /// <inheritdoc />
    public async Task ProcessTestCompletion(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        // Validate input parameters
        ValidateParameters(message, cancellationToken);

        try
        {
            // Pre-processing hook - allows derived classes to perform setup operations
            await OnProcessingStarted(message, cancellationToken).ConfigureAwait(false);

            // Core processing logic - implemented by derived classes
            await ProcessTestCompletionCore(message, cancellationToken).ConfigureAwait(false);

            // Post-processing hook - allows derived classes to perform cleanup or additional operations
            await OnProcessingCompleted(message, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected behavior, log as warning and re-throw
            _logger.Log(LogLevel.Warning, 
                $"Processing test completion message for test case '{message.TestCaseId}' was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            // Handle processing errors and provide opportunity for derived classes to respond
            await OnProcessingFailed(message, ex, cancellationToken).ConfigureAwait(false);
            
            // Log the error and re-throw to maintain exception contract
            _logger.Log(LogLevel.Error, 
                $"Failed to process test completion message for test case '{message.TestCaseId}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Processes the test completion message with processor-specific logic.
    /// This is the core processing method that derived classes must implement.
    /// </summary>
    /// <param name="message">
    /// The test completion message containing all relevant test execution data,
    /// including performance metrics, test results, and metadata.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the processing operation.
    /// The operation should be cancelled gracefully, ensuring proper cleanup
    /// and resource disposal.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous processing operation.
    /// The task completes when the message has been successfully processed.
    /// </returns>
    /// <remarks>
    /// This method contains the processor-specific logic for handling test completion
    /// messages. Implementations should:
    /// 
    /// - Process the message according to their specific requirements
    /// - Handle errors gracefully without stopping the queue processing
    /// - Use the cancellation token to support graceful shutdown
    /// - Consider batching and cross-test-case analysis requirements
    /// - Be thread-safe and handle concurrent execution scenarios
    /// 
    /// The base class handles parameter validation, error logging, and lifecycle
    /// management, so derived classes can focus on their core processing logic.
    /// </remarks>
    protected abstract Task ProcessTestCompletionCore(TestCompletionQueueMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Called before the core processing logic is executed.
    /// Derived classes can override this method to perform setup operations.
    /// </summary>
    /// <param name="message">
    /// The test completion message that will be processed.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous pre-processing operation.
    /// </returns>
    /// <remarks>
    /// The default implementation logs the start of processing at the Information level.
    /// Derived classes can override this method to perform processor-specific setup
    /// operations such as initializing resources, validating processor-specific
    /// requirements, or preparing for batch processing.
    /// </remarks>
    protected virtual Task OnProcessingStarted(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information, 
            $"Starting processing of test completion message for test case '{message.TestCaseId}'");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after the core processing logic has completed successfully.
    /// Derived classes can override this method to perform cleanup or additional operations.
    /// </summary>
    /// <param name="message">
    /// The test completion message that was processed.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous post-processing operation.
    /// </returns>
    /// <remarks>
    /// The default implementation logs the successful completion of processing at the Information level.
    /// Derived classes can override this method to perform processor-specific cleanup
    /// operations such as releasing resources, updating metrics, or triggering
    /// additional processing steps.
    /// </remarks>
    protected virtual Task OnProcessingCompleted(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information, 
            $"Successfully completed processing of test completion message for test case '{message.TestCaseId}'");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when an error occurs during processing.
    /// Derived classes can override this method to handle processor-specific error scenarios.
    /// </summary>
    /// <param name="message">
    /// The test completion message that was being processed when the error occurred.
    /// </param>
    /// <param name="exception">
    /// The exception that occurred during processing.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous error handling operation.
    /// </returns>
    /// <remarks>
    /// The default implementation logs the error at the Error level with exception details.
    /// Derived classes can override this method to perform processor-specific error
    /// handling such as retry logic, fallback operations, or error reporting to
    /// external systems. Note that the base class will still re-throw the exception
    /// after this method completes to maintain the exception contract.
    /// </remarks>
    protected virtual Task OnProcessingFailed(TestCompletionQueueMessage message, Exception exception, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Error, 
            $"Processing failed for test case '{message.TestCaseId}' with exception: {exception.GetType().Name} - {exception.Message}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the input parameters for the ProcessTestCompletion method.
    /// </summary>
    /// <param name="message">The test completion message to validate.</param>
    /// <param name="cancellationToken">The cancellation token to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message"/> is null.
    /// </exception>
    /// <remarks>
    /// This method performs standard parameter validation that is common to all
    /// processor implementations. The cancellation token is not validated as
    /// null cancellation tokens are valid and represent no cancellation.
    /// </remarks>
    private static void ValidateParameters(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        // Note: CancellationToken is a value type and cannot be null
        // No additional validation needed for cancellationToken
    }
}
