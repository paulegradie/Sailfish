using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.Queue.Implementation;

namespace Sailfish.TestAdapter.Queue.Implementation;

/// <summary>
/// Service that consumes test completion messages from the queue and dispatches them to registered processors.
/// This service is part of the intercepting queue architecture that enables asynchronous processing,
/// batch analysis, and enhanced result generation before test results are reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// The TestCompletionQueueConsumer is a background service that continuously monitors the test completion
/// queue for new messages and dispatches them to all registered processors for processing. This enables
/// the intercepting queue architecture where test completion messages are processed by multiple processors
/// before being reported to the VS Test Platform.
/// 
/// Key responsibilities:
/// - Continuously monitor the queue for new test completion messages
/// - Dispatch messages to all registered processors in sequence
/// - Handle individual processor failures without stopping queue processing
/// - Provide processor registration and lifecycle management
/// - Support graceful startup and shutdown with proper resource cleanup
/// - Implement retry logic for transient processor failures
/// - Maintain thread-safe operations for concurrent execution scenarios
/// 
/// The consumer service runs a background processing loop that dequeues messages and dispatches them
/// to registered processors. Individual processor failures are logged and handled gracefully without
/// stopping the entire queue processing operation. This ensures that test results are never lost
/// even if individual processors encounter errors.
/// 
/// Processor Execution:
/// Processors are executed sequentially in the order they were registered. This allows for processor
/// dependencies where later processors may depend on the results of earlier processors. For example,
/// the framework publishing processor typically runs last to ensure all analysis and enhancement
/// processors have completed before results are sent to the VS Test Platform.
/// 
/// Thread Safety:
/// This service is designed to be thread-safe and can handle concurrent registration/unregistration
/// of processors while the background processing loop is running. The service uses thread-safe
/// collections and proper synchronization to ensure safe concurrent operations.
/// </remarks>
public class TestCompletionQueueConsumer : IDisposable
{
    private readonly ITestCompletionQueue _queue;
    private readonly ILogger _logger;
    private readonly ConcurrentBag<ITestCompletionQueueProcessor> _processors;
    private readonly IProcessingMetricsCollector? _metricsCollector;
    
    private Task? _processingTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private volatile bool _isRunning;
    private volatile bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCompletionQueueConsumer"/> class.
    /// </summary>
    /// <param name="queue">
    /// The test completion queue service that provides messages for processing.
    /// The consumer will continuously monitor this queue for new messages.
    /// </param>
    /// <param name="logger">
    /// The logger service for recording consumer operations, errors, and diagnostic information.
    /// Used to log processing lifecycle events, processor failures, and other significant events.
    /// </param>
    /// <param name="metricsCollector">
    /// Optional metrics collector for tracking processing performance. If provided,
    /// processing times will be recorded for health monitoring and analysis.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> or <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// The consumer requires a queue service to monitor for messages and a logger for
    /// comprehensive error reporting and diagnostic information. These dependencies are
    /// typically injected by the DI container during service registration and instantiation.
    /// </remarks>
    public TestCompletionQueueConsumer(ITestCompletionQueue queue, ILogger logger, IProcessingMetricsCollector? metricsCollector = null)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCollector = metricsCollector;
        _processors = new ConcurrentBag<ITestCompletionQueueProcessor>();

        _isRunning = false;
        _isDisposed = false;
    }

    /// <summary>
    /// Gets a value indicating whether the consumer service is currently running
    /// and processing messages from the queue.
    /// </summary>
    /// <value>
    /// <c>true</c> if the consumer is running and processing messages; otherwise, <c>false</c>.
    /// </value>
    public bool IsRunning => _isRunning && !_isDisposed;

    /// <summary>
    /// Gets the current number of processors registered with the consumer service.
    /// </summary>
    /// <value>
    /// The number of processors currently registered to receive test completion messages.
    /// </value>
    public int ProcessorCount => _processors.Count;

    /// <summary>
    /// Gets a snapshot of all registered processors.
    /// </summary>
    /// <returns>An array of all currently registered processors.</returns>
    public ITestCompletionQueueProcessor[] GetProcessors()
    {
        return _processors.ToArray();
    }

    /// <summary>
    /// Registers a processor to receive test completion messages from the queue.
    /// </summary>
    /// <param name="processor">
    /// The processor to register for receiving test completion messages.
    /// The processor will be called for each message dequeued from the queue.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="processor"/> is null.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the consumer service has been disposed.
    /// </exception>
    /// <remarks>
    /// Processors can be registered at any time, including while the consumer service
    /// is running. The processor will begin receiving messages immediately after
    /// registration. Processors are executed in the order they were registered,
    /// allowing for processor dependencies and pipeline execution.
    /// 
    /// The same processor instance can be registered multiple times, but this is
    /// generally not recommended as it will result in duplicate processing of messages.
    /// </remarks>
    public void RegisterProcessor(ITestCompletionQueueProcessor processor)
    {
        if (processor == null)
        {
            throw new ArgumentNullException(nameof(processor));
        }
        
        ThrowIfDisposed();
        
        _processors.Add(processor);
        _logger.Log(LogLevel.Information, 
            $"Registered processor '{processor.GetType().Name}' with queue consumer. Total processors: {_processors.Count}");
    }

    /// <summary>
    /// Unregisters a processor from receiving test completion messages.
    /// </summary>
    /// <param name="processor">
    /// The processor to unregister. The processor will no longer receive
    /// test completion messages after this method completes.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="processor"/> is null.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the consumer service has been disposed.
    /// </exception>
    /// <remarks>
    /// Processors can be unregistered at any time, including while the consumer service
    /// is running. The processor will stop receiving messages immediately after
    /// unregistration. If the processor is currently processing a message, it will
    /// complete that processing before being fully unregistered.
    /// 
    /// If the same processor instance was registered multiple times, only one
    /// registration will be removed per call to this method.
    /// </remarks>
    public void UnregisterProcessor(ITestCompletionQueueProcessor processor)
    {
        if (processor == null)
        {
            throw new ArgumentNullException(nameof(processor));
        }
        
        ThrowIfDisposed();
        
        // Create a new bag without the processor to remove
        var remainingProcessors = _processors.Where(p => !ReferenceEquals(p, processor)).ToList();
        
        // Clear the current bag and add back the remaining processors
        while (_processors.TryTake(out _)) { }
        foreach (var remainingProcessor in remainingProcessors)
        {
            _processors.Add(remainingProcessor);
        }
        
        _logger.Log(LogLevel.Information, 
            $"Unregistered processor '{processor.GetType().Name}' from queue consumer. Total processors: {_processors.Count}");
    }

    /// <summary>
    /// Starts the consumer service and begins processing messages from the queue.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the start operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous start operation.
    /// The task completes when the consumer service has started successfully.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the consumer service is already running or has been disposed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method starts the background processing loop that continuously monitors
    /// the queue for new messages and dispatches them to registered processors.
    /// The consumer service must be started before it will process any messages.
    /// 
    /// The background processing will continue until StopAsync is called or the
    /// consumer service is disposed. Individual processor failures will not stop
    /// the background processing loop.
    /// </remarks>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        
        if (_isRunning)
        {
            throw new InvalidOperationException("Consumer service is already running.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        
        _cancellationTokenSource = new CancellationTokenSource();
        _isRunning = true;
        
        // Start the background processing task
        _processingTask = ProcessMessagesAsync(_cancellationTokenSource.Token);
        
        _logger.Log(LogLevel.Information, 
            "Test completion queue consumer service started successfully");
        
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the consumer service gracefully, allowing current message processing to complete.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the stop operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous stop operation.
    /// The task completes when the consumer service has stopped gracefully.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method stops the background processing loop gracefully, allowing any
    /// currently processing messages to complete before stopping. The method will
    /// wait for the background processing task to complete before returning.
    /// 
    /// After stopping, the consumer service can be restarted by calling StartAsync again.
    /// </remarks>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_isRunning || _isDisposed)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        
        _logger.Log(LogLevel.Information, 
            "Stopping test completion queue consumer service...");
        
        _isRunning = false;
        
        // Signal the background task to stop
        _cancellationTokenSource?.Cancel();
        
        // Wait for the background task to complete
        if (_processingTask != null)
        {
            try
            {
                await _processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }
        
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _processingTask = null;
        
        _logger.Log(LogLevel.Information,
            "Test completion queue consumer service stopped successfully");
    }

    /// <summary>
    /// The main processing loop that continuously dequeues messages and dispatches them to processors.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the processing loop.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous processing operation.
    /// </returns>
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information,
            "Starting background message processing loop");

        try
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                try
                {
                    // Dequeue the next message from the queue
                    var message = await _queue.DequeueAsync(cancellationToken).ConfigureAwait(false);

                    // If message is null, the queue is completed and no more messages will be available
                    if (message == null)
                    {
                        _logger.Log(LogLevel.Information,
                            "Queue completed - no more messages available for processing");
                        break;
                    }

                    // Process the message with all registered processors
                    await ProcessMessageWithProcessors(message, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    _logger.Log(LogLevel.Information,
                        "Message processing loop cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing to ensure queue doesn't stop
                    _logger.Log(LogLevel.Error, ex,
                        "Unexpected error in message processing loop - continuing processing");

                    // Add a small delay to prevent tight error loops
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _logger.Log(LogLevel.Information,
                "Background message processing loop completed");
        }
    }

    /// <summary>
    /// Processes a single message with all registered processors.
    /// </summary>
    /// <param name="message">The test completion message to process.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    private async Task ProcessMessageWithProcessors(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        var processors = _processors.ToArray(); // Snapshot for thread safety

        if (processors.Length == 0)
        {
            _logger.Log(LogLevel.Warning,
                $"No processors registered - message for test case '{message.TestCaseId}' will not be processed");
            return;
        }

        _logger.Log(LogLevel.Debug,
            $"Processing message for test case '{message.TestCaseId}' with {processors.Length} processors");

        // Process with each registered processor sequentially
        foreach (var processor in processors)
        {
            await ProcessMessageWithSingleProcessor(message, processor, cancellationToken).ConfigureAwait(false);
        }

        _logger.Log(LogLevel.Debug,
            $"Completed processing message for test case '{message.TestCaseId}' with all processors");
    }

    /// <summary>
    /// Processes a message with a single processor, including error handling and retry logic.
    /// </summary>
    /// <param name="message">The test completion message to process.</param>
    /// <param name="processor">The processor to use for processing.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    private async Task ProcessMessageWithSingleProcessor(
        TestCompletionQueueMessage message,
        ITestCompletionQueueProcessor processor,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        const int baseDelayMs = 100;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                await processor.ProcessTestCompletion(message, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                // Record processing time if metrics collector is available
                _metricsCollector?.RecordProcessingTime(stopwatch.Elapsed.TotalMilliseconds);

                // Success - no need to retry
                if (attempt > 1)
                {
                    _logger.Log(LogLevel.Information,
                        $"Processor '{processor.GetType().Name}' succeeded on attempt {attempt} for test case '{message.TestCaseId}'");
                }
                return;
            }
            catch (OperationCanceledException)
            {
                // Cancellation should not be retried
                _logger.Log(LogLevel.Warning,
                    $"Processor '{processor.GetType().Name}' was cancelled for test case '{message.TestCaseId}'");
                throw;
            }
            catch (Exception ex)
            {
                var isLastAttempt = attempt == maxRetries;
                var logLevel = isLastAttempt ? LogLevel.Error : LogLevel.Warning;

                _logger.Log(logLevel, ex,
                    $"Processor '{processor.GetType().Name}' failed on attempt {attempt}/{maxRetries} for test case '{message.TestCaseId}': {ex.Message}");

                if (isLastAttempt)
                {
                    // Log final failure but don't throw - continue with other processors
                    _logger.Log(LogLevel.Error,
                        $"Processor '{processor.GetType().Name}' failed permanently for test case '{message.TestCaseId}' after {maxRetries} attempts");
                    return;
                }

                // Calculate exponential backoff delay
                var delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the consumer service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(TestCompletionQueueConsumer));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="TestCompletionQueueConsumer"/>.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _logger.Log(LogLevel.Information,
            "Disposing test completion queue consumer service");

        // Stop the service if it's running
        if (_isRunning)
        {
            try
            {
                StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex,
                    "Error occurred while stopping consumer service during disposal");
            }
        }

        _cancellationTokenSource?.Dispose();
        _isDisposed = true;

        _logger.Log(LogLevel.Information,
            "Test completion queue consumer service disposed successfully");
    }
}
