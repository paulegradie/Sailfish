using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;
using Sailfish.TestAdapter.Queue.Processors;

namespace Sailfish.TestAdapter.Queue.Implementation;

/// <summary>
/// High-level service for managing the lifecycle of the test completion queue system
/// in the intercepting queue architecture. This manager coordinates the creation, startup,
/// and shutdown of queue and consumer services to enable asynchronous processing,
/// batch analysis, and cross-test-case comparison before test results reach the VS Test Platform.
/// </summary>
/// <remarks>
/// The TestCompletionQueueManager serves as the central orchestrator for the queue infrastructure,
/// providing a simplified interface for managing the complex lifecycle of multiple queue components.
/// This manager abstracts the coordination between the queue factory, queue instance, consumer service,
/// and processor registration to provide a cohesive queue management experience.
/// 
/// Key responsibilities:
/// - Create queue instances using the configured factory
/// - Coordinate queue and consumer lifecycle (startup, shutdown, completion)
/// - Manage processor registration with the consumer service
/// - Provide thread-safe operations for concurrent queue management scenarios
/// - Ensure proper resource cleanup and disposal of all queue components
/// - Support graceful shutdown with completion detection for batch processing
/// - Integrate with existing Sailfish logging infrastructure for comprehensive diagnostics
/// 
/// The manager follows the intercepting queue architecture where test completion messages
/// are processed by multiple processors before being reported to the VS Test Platform.
/// This enables advanced features like batch processing, cross-test-case analysis,
/// and enhanced result generation.
/// 
/// Lifecycle Management:
/// The manager coordinates the startup and shutdown of both the queue and consumer services.
/// During startup, it creates a queue instance using the factory, creates a consumer service,
/// and starts both components. During shutdown, it ensures graceful completion of message
/// processing before stopping the consumer and queue.
/// 
/// Thread Safety:
/// This manager is designed to be thread-safe and can handle concurrent queue management
/// operations. Thread safety is achieved through proper locking mechanisms and coordination
/// of the underlying queue and consumer components.
/// 
/// Integration:
/// The manager integrates with the existing Sailfish dependency injection system and
/// can be registered as a service in the DI container. It uses the standard ILogger
/// interface for comprehensive diagnostic information and error reporting.
/// </remarks>
public class TestCompletionQueueManager : IDisposable
{
    private readonly ITestCompletionQueue _queue;
    private readonly ITestCompletionQueueProcessor[] _processors;
    private readonly ILogger _logger;
    private readonly object _lock = new object();

    private IProcessingMetricsCollector? _metricsCollector;

    private TestCompletionQueueConsumer? _consumer;
    private bool _isRunning;
    private bool _isDisposed;

    /// <summary>
    /// Gets a value indicating whether the queue manager is currently running.
    /// </summary>
    /// <value>
    /// <c>true</c> if the queue manager is running and ready to process messages;
    /// <c>false</c> if the manager is stopped or not yet started.
    /// </value>
    /// <remarks>
    /// This property provides thread-safe access to the running state of the queue manager.
    /// When true, the manager has successfully started both the queue and consumer services
    /// and is ready to process test completion messages. When false, the manager is either
    /// not yet started or has been stopped.
    /// </remarks>
    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning && !_isDisposed;
            }
        }
    }

    /// <summary>
    /// Sets the metrics collector for tracking processing performance.
    /// This method allows late binding of the metrics collector to avoid circular dependencies.
    /// </summary>
    /// <param name="metricsCollector">The metrics collector to use for tracking processing times.</param>
    /// <remarks>
    /// This method is typically called during dependency injection setup to establish
    /// the connection between the queue manager and health monitoring systems.
    /// </remarks>
    public void SetMetricsCollector(IProcessingMetricsCollector? metricsCollector)
    {
        lock (_lock)
        {
            _metricsCollector = metricsCollector;
        }
    }

    /// <summary>
    /// Gets the current queue instance managed by this manager.
    /// </summary>
    /// <value>
    /// The current <see cref="ITestCompletionQueue"/> instance, or <c>null</c> if the manager
    /// is not running or has been disposed.
    /// </value>
    /// <remarks>
    /// This property provides access to the queue instance for publishing test completion
    /// messages. The queue instance is injected during construction and managed throughout
    /// the manager's lifecycle. Callers should check that the manager is running before using the queue instance.
    /// </remarks>
    public ITestCompletionQueue? Queue
    {
        get
        {
            lock (_lock)
            {
                return _isRunning && !_isDisposed ? _queue : null;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCompletionQueueManager"/> class.
    /// </summary>
    /// <param name="queue">
    /// The queue instance to manage. This should be the same singleton instance used by
    /// the publisher to ensure consistent queue state across the system.
    /// </param>
    /// <param name="processors">
    /// The collection of processors to register with the queue consumer for message processing.
    /// These processors will handle test completion messages from the queue.
    /// </param>
    /// <param name="logger">
    /// The logger service for recording manager operations, lifecycle events,
    /// and diagnostic information. Used for troubleshooting queue management issues
    /// and monitoring manager usage.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/>, <paramref name="processors"/>, or <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// The manager uses the provided queue instance directly instead of creating a new one.
    /// This ensures that the manager and publisher work with the same queue instance,
    /// preventing race conditions and state inconsistencies. The processors are registered
    /// with the consumer during startup to enable message processing.
    ///
    /// A metrics collector can be set later using SetMetricsCollector() to avoid circular dependencies.
    /// </remarks>
    public TestCompletionQueueManager(ITestCompletionQueue queue, ITestCompletionQueueProcessor[] processors, ILogger logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _processors = processors ?? throw new ArgumentNullException(nameof(processors));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _isRunning = false;
        _isDisposed = false;
    }

    /// <summary>
    /// Starts the queue manager by creating and starting the queue and consumer services.
    /// </summary>
    /// <param name="configuration">
    /// The configuration settings for the queue system, including capacity, timeout,
    /// and processor settings. Used to configure the created queue instance.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the startup operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous startup operation.
    /// The task completes when both the queue and consumer services have been
    /// successfully started and are ready to process messages.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the manager is already running or has been disposed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method coordinates the startup of the entire queue system by creating a queue
    /// instance using the factory, creating a consumer service, and starting both components.
    /// The method ensures that both the queue and consumer are successfully started before
    /// marking the manager as running.
    /// 
    /// If startup fails at any point, the method will attempt to clean up any partially
    /// created resources before throwing the exception.
    /// </remarks>
    public async Task StartAsync(QueueConfiguration configuration, CancellationToken cancellationToken)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        lock (_lock)
        {
            ThrowIfDisposed();
            
            if (_isRunning)
            {
                throw new InvalidOperationException("Queue manager is already running. Call StopAsync before starting again.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        _logger.Log(LogLevel.Information, 
            "Starting test completion queue manager with configuration: Enabled={0}, MaxCapacity={1}, BatchProcessing={2}",
            configuration.IsEnabled, configuration.MaxQueueCapacity, configuration.EnableBatchProcessing);

        try
        {
            // Create the consumer service using the injected queue instance
            var consumer = new TestCompletionQueueConsumer(_queue, _logger, _metricsCollector);

            // Register all processors with the consumer
            foreach (var processor in _processors)
            {
                consumer.RegisterProcessor(processor);
                _logger.Log(LogLevel.Debug, "Registered processor: {0}", processor.GetType().Name);
            }

            _logger.Log(LogLevel.Information, "Registered {0} processors with queue consumer", _processors.Length);

            // Start the queue
            await _queue.StartAsync(cancellationToken).ConfigureAwait(false);

            // Start the consumer
            await consumer.StartAsync(cancellationToken).ConfigureAwait(false);

            lock (_lock)
            {
                _consumer = consumer;
                _isRunning = true;
            }

            _logger.Log(LogLevel.Information,
                "Test completion queue manager started successfully");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to start test completion queue manager: {0}", ex.Message);
            
            // Clean up any partially created resources
            await CleanupResourcesAsync().ConfigureAwait(false);
            
            throw;
        }
    }

    /// <summary>
    /// Stops the queue manager by gracefully shutting down the consumer and queue services.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the shutdown operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous shutdown operation.
    /// The task completes when both the consumer and queue services have been
    /// successfully stopped and all resources have been cleaned up.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method coordinates the graceful shutdown of the entire queue system by stopping
    /// the consumer service first (to stop processing new messages) and then stopping the
    /// queue service. The method ensures that all pending messages are processed before
    /// completing the shutdown.
    /// 
    /// If the manager is not running, this method returns immediately without performing
    /// any operations. If shutdown fails at any point, the method will log the error
    /// but continue with the shutdown process to ensure resources are cleaned up.
    /// </remarks>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ITestCompletionQueue? queue;
        TestCompletionQueueConsumer? consumer;
        
        lock (_lock)
        {
            if (!_isRunning || _isDisposed)
            {
                return;
            }
            
            queue = _queue;
            consumer = _consumer;
            _isRunning = false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        _logger.Log(LogLevel.Information, 
            "Stopping test completion queue manager...");

        try
        {
            // Stop the consumer first to stop processing new messages
            if (consumer != null)
            {
                await consumer.StopAsync(cancellationToken).ConfigureAwait(false);
            }

            // Stop the queue
            if (queue != null)
            {
                await queue.StopAsync(cancellationToken).ConfigureAwait(false);
            }

            // Process any completed batches for method comparisons
            await ProcessCompletedBatchesForComparisons(cancellationToken).ConfigureAwait(false);

            _logger.Log(LogLevel.Information,
                "Test completion queue manager stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Error occurred while stopping test completion queue manager: {0}", ex.Message);
            throw;
        }
        finally
        {
            // Clean up resources
            await CleanupResourcesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Registers a processor with the queue consumer for processing test completion messages.
    /// </summary>
    /// <param name="processor">
    /// The processor to register with the consumer service. This processor will receive
    /// all test completion messages for processing as part of the queue processing pipeline.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="processor"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the manager is not running or has been disposed.
    /// </exception>
    /// <remarks>
    /// This method registers a processor with the consumer service to participate in the
    /// queue processing pipeline. Processors are executed in the order they are registered,
    /// allowing for processor dependencies and pipeline execution.
    ///
    /// The manager must be running (StartAsync must have been called successfully) before
    /// processors can be registered. Processors can be registered at any time while the
    /// manager is running.
    ///
    /// Common processor types include framework publishing processors, comparison processors,
    /// batch completion processors, and analysis processors.
    /// </remarks>
    public void RegisterProcessor(ITestCompletionQueueProcessor processor)
    {
        if (processor == null)
        {
            throw new ArgumentNullException(nameof(processor));
        }

        TestCompletionQueueConsumer? consumer;

        lock (_lock)
        {
            ThrowIfDisposed();

            if (!_isRunning)
            {
                throw new InvalidOperationException("Queue manager is not running. Call StartAsync before registering processors.");
            }

            consumer = _consumer;
        }

        if (consumer == null)
        {
            throw new InvalidOperationException("Consumer service is not available. Ensure the manager is properly started.");
        }

        consumer.RegisterProcessor(processor);

        _logger.Log(LogLevel.Information,
            "Registered processor '{0}' with queue manager", processor.GetType().Name);
    }

    /// <summary>
    /// Marks the queue as complete and waits for all pending messages to be processed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the completion operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous completion operation.
    /// The task completes when the queue has been marked as complete and all
    /// pending messages have been processed by the consumer service.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the manager is not running or has been disposed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method signals that no more test completion messages will be added to the queue
    /// and waits for all pending messages to be processed before returning. This is typically
    /// called at the end of test execution to ensure all test results are processed before
    /// the test run completes.
    ///
    /// The method coordinates with both the queue and consumer services to ensure graceful
    /// completion of message processing. After completion, no new messages can be added to
    /// the queue, but the manager remains running until StopAsync is called.
    /// </remarks>
    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        ITestCompletionQueue? queue;

        lock (_lock)
        {
            ThrowIfDisposed();

            if (!_isRunning)
            {
                throw new InvalidOperationException("Queue manager is not running. Call StartAsync before completing the queue.");
            }

            queue = _queue;
        }

        if (queue == null)
        {
            throw new InvalidOperationException("Queue service is not available. Ensure the manager is properly started.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _logger.Log(LogLevel.Information,
            "Completing test completion queue and waiting for message processing to finish...");

        try
        {
            // Mark the queue as complete - no more messages can be added
            await queue.CompleteAsync(cancellationToken).ConfigureAwait(false);

            _logger.Log(LogLevel.Information,
                "Test completion queue completed successfully - all pending messages will be processed");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Error occurred while completing test completion queue: {0}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Cleans up queue and consumer resources.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous cleanup operation.
    /// </returns>
    private async Task CleanupResourcesAsync()
    {
        ITestCompletionQueue? queue;
        TestCompletionQueueConsumer? consumer;

        lock (_lock)
        {
            queue = _queue;
            consumer = _consumer;
            _consumer = null;
        }

        // Dispose consumer first
        if (consumer != null)
        {
            try
            {
                consumer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex,
                    "Error occurred while disposing consumer service: {0}", ex.Message);
            }
        }

        // Dispose queue
        if (queue != null)
        {
            try
            {
                if (queue is IDisposable disposableQueue)
                {
                    disposableQueue.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex,
                    "Error occurred while disposing queue service: {0}", ex.Message);
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Processes completed batches for method comparisons after queue processing is complete.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessCompletedBatchesForComparisons(CancellationToken cancellationToken)
    {
        try
        {
            _logger.Log(LogLevel.Information, "Processing completed batches for method comparisons...");

            // Get the MethodComparisonProcessor from the consumer's processors
            var consumer = _consumer;
            if (consumer != null)
            {
                var processors = consumer.GetProcessors();
                var comparisonProcessor = processors.OfType<MethodComparisonProcessor>().FirstOrDefault();

                if (comparisonProcessor != null)
                {
                    await comparisonProcessor.ProcessCompletedBatchesAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.Log(LogLevel.Debug, "No MethodComparisonProcessor found - skipping batch comparison processing");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to process completed batches for method comparisons: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the manager has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the manager has been disposed.
    /// </exception>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(TestCompletionQueueManager));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="TestCompletionQueueManager"/>.
    /// </summary>
    /// <remarks>
    /// This method ensures proper cleanup of all queue manager resources by stopping
    /// the manager if it's running and disposing of all managed resources. The method
    /// is safe to call multiple times and will not throw exceptions during disposal.
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _logger.Log(LogLevel.Information,
            "Disposing test completion queue manager");

        // Stop the manager if it's running
        if (_isRunning)
        {
            try
            {
                StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex,
                    "Error occurred while stopping queue manager during disposal: {0}", ex.Message);
            }
        }

        // Clean up any remaining resources
        try
        {
            CleanupResourcesAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Error occurred while cleaning up resources during disposal: {0}", ex.Message);
        }

        lock (_lock)
        {
            _isDisposed = true;
        }

        _logger.Log(LogLevel.Information,
            "Test completion queue manager disposed successfully");
    }
}
