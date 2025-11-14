using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Implementation;

/// <summary>
/// Service that monitors and handles timeout scenarios for incomplete test case batches.
/// This service is part of the intercepting queue architecture that enables batch processing
/// and cross-test-case analysis before test results are reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// The BatchTimeoutHandler operates as a background service that periodically monitors all
/// pending batches for timeout conditions and processes them when timeouts are detected.
/// This ensures that test results are eventually reported to the VS Test Platform even
/// when batches don't complete naturally due to missing test cases or other issues.
/// 
/// Key responsibilities:
/// - Monitor batch completion timeouts using configurable intervals
/// - Detect batches that have exceeded their completion timeout
/// - Process timed-out batches by publishing framework notifications
/// - Update batch status to TimedOut for proper tracking
/// - Provide comprehensive logging and error reporting
/// - Support graceful startup and shutdown with proper resource cleanup
/// 
/// The service uses a timer-based approach to periodically check for timed-out batches
/// and processes them using the same framework publishing mechanism as normal batch completion.
/// This maintains consistency with the existing queue architecture while providing timeout
/// handling capabilities.
/// 
/// Thread Safety:
/// All operations are thread-safe and designed to handle concurrent execution scenarios
/// where multiple batches may timeout simultaneously or during active test execution.
/// </remarks>
internal class BatchTimeoutHandler : IBatchTimeoutHandler, IDisposable
{
    private readonly ITestCaseBatchingService _batchingService;
    private readonly IMediator _mediator;
    private readonly QueueConfiguration _configuration;
    private readonly ILogger _logger;
    
    private Timer? _monitoringTimer;
    private bool _isRunning;
    private bool _isDisposed;
    private readonly object _lockObject = new();
    
    /// <summary>
    /// Initializes a new instance of the BatchTimeoutHandler class.
    /// </summary>
    /// <param name="batchingService">The batching service to monitor for timed-out batches.</param>
    /// <param name="mediator">The mediator for publishing framework notifications.</param>
    /// <param name="configuration">The queue configuration containing timeout settings.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the required parameters are null.
    /// </exception>
    public BatchTimeoutHandler(
        ITestCaseBatchingService batchingService,
        IMediator mediator,
        QueueConfiguration configuration,
        ILogger logger)
    {
        _batchingService = batchingService ?? throw new ArgumentNullException(nameof(batchingService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lockObject)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Batch timeout handler is already running.");
            }

            _isRunning = true;
        }

        try
        {
            // Calculate monitoring interval (default to 30 seconds or use configuration)
            var monitoringInterval = TimeSpan.FromSeconds(30);
            if (_configuration.BatchCompletionTimeoutMs > 0)
            {
                // Use a monitoring interval that's 1/4 of the batch timeout, but at least 10 seconds
                var calculatedInterval = TimeSpan.FromMilliseconds(_configuration.BatchCompletionTimeoutMs / 4.0);
                monitoringInterval = calculatedInterval < TimeSpan.FromSeconds(10) 
                    ? TimeSpan.FromSeconds(10) 
                    : calculatedInterval;
            }

            // Start the monitoring timer
            _monitoringTimer = new Timer(
                MonitorBatchTimeouts,
                null,
                monitoringInterval,
                monitoringInterval);

            _logger.Log(LogLevel.Information,
                "Batch timeout handler started with monitoring interval of {0} seconds",
                monitoringInterval.TotalSeconds);

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            lock (_lockObject)
            {
                _isRunning = false;
            }

            _logger.Log(LogLevel.Error, ex,
                "Failed to start batch timeout handler: {0}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lockObject)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
        }

        try
        {
            // Stop the monitoring timer
            if (_monitoringTimer != null)
            {
                await _monitoringTimer.DisposeAsync().ConfigureAwait(false);
                _monitoringTimer = null;
            }

            // Process any remaining timed-out batches before stopping
            await ProcessTimedOutBatchesAsync(cancellationToken).ConfigureAwait(false);

            _logger.Log(LogLevel.Information,
                "Batch timeout handler stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Error occurred while stopping batch timeout handler: {0}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> ProcessTimedOutBatchesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Get all pending batches
            var pendingBatches = await _batchingService.GetPendingBatchesAsync(cancellationToken).ConfigureAwait(false);
            var timedOutBatches = new List<TestCaseBatch>();

            // Check each batch for timeout
            foreach (var batch in pendingBatches)
            {
                if (IsBatchTimedOut(batch))
                {
                    timedOutBatches.Add(batch);
                }
            }

            if (timedOutBatches.Count == 0)
            {
                _logger.Log(LogLevel.Debug, "No timed-out batches found during timeout check");
                return 0;
            }

            _logger.Log(LogLevel.Warning,
                "Found {0} timed-out batches that will be processed", timedOutBatches.Count);

            // Process each timed-out batch
            var processedCount = 0;
            foreach (var batch in timedOutBatches)
            {
                try
                {
                    await ProcessTimedOutBatch(batch, cancellationToken).ConfigureAwait(false);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, ex,
                        "Failed to process timed-out batch '{0}': {1}", batch.BatchId, ex.Message);
                    // Continue processing other batches
                }
            }

            _logger.Log(LogLevel.Information,
                "Processed {0} of {1} timed-out batches successfully", processedCount, timedOutBatches.Count);

            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Error occurred while processing timed-out batches: {0}", ex.Message);
            return 0;
        }
    }

    /// <summary>
    /// Timer callback method that periodically checks for and processes timed-out batches.
    /// </summary>
    /// <param name="state">Timer state (not used).</param>
    private async void MonitorBatchTimeouts(object? state)
    {
        if (_isDisposed || !_isRunning)
        {
            return;
        }

        try
        {
            await ProcessTimedOutBatchesAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Error occurred during batch timeout monitoring: {0}", ex.Message);
            // Don't re-throw in timer callback to prevent timer from stopping
        }
    }

    /// <summary>
    /// Determines if a batch has timed out based on its creation time and timeout configuration.
    /// </summary>
    /// <param name="batch">The batch to check for timeout.</param>
    /// <returns>True if the batch has timed out, false otherwise.</returns>
    private bool IsBatchTimedOut(TestCaseBatch batch)
    {
        if (batch.Status != BatchStatus.Pending)
        {
            return false;
        }

        // Use batch-specific timeout if available, otherwise use global configuration
        var timeout = batch.CompletionTimeout ?? TimeSpan.FromMilliseconds(_configuration.BatchCompletionTimeoutMs);
        var elapsed = DateTime.UtcNow - batch.CreatedAt;

        return elapsed >= timeout;
    }

    /// <summary>
    /// Processes a single timed-out batch by publishing framework notifications for all test cases.
    /// </summary>
    /// <param name="batch">The timed-out batch to process.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    private async Task ProcessTimedOutBatch(TestCaseBatch batch, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Warning,
            "Processing timed-out batch '{0}' containing {1} test cases (created {2:yyyy-MM-dd HH:mm:ss} UTC)",
            batch.BatchId, batch.TestCases.Count, batch.CreatedAt);

        // Update batch status to TimedOut
        lock (batch)
        {
            batch.Status = BatchStatus.TimedOut;
            batch.CompletedAt = DateTime.UtcNow;
        }

        // Publish framework notifications for each test case in the batch
        foreach (var testCase in batch.TestCases)
        {
            try
            {
                await PublishFrameworkNotification(testCase, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex,
                    "Failed to publish framework notification for timed-out test case '{0}': {1}",
                    testCase.TestCaseId, ex.Message);
                // Continue processing other test cases
            }
        }

        _logger.Log(LogLevel.Information,
            "Successfully processed timed-out batch '{0}' with {1} test cases",
            batch.BatchId, batch.TestCases.Count);
    }

    /// <summary>
    /// Publishes a framework notification for a test case from a timed-out batch.
    /// </summary>
    /// <param name="message">The test completion message to publish.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous publishing operation.</returns>
    private async Task PublishFrameworkNotification(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Extract required data from the message metadata (similar to FrameworkPublishingProcessor)
            var testCase = ExtractTestCase(message);
            var testOutputMessage = ExtractTestOutputMessage(message);
            var startTime = ExtractStartTime(message);
            var endTime = ExtractEndTime(message);
            var duration = CalculateDuration(message, startTime, endTime);
            var statusCode = DetermineStatusCode(message);
            var exception = ExtractException(message);

            // Create the framework notification
            var frameworkNotification = new FrameworkTestCaseEndNotification(
                testOutputMessage,
                startTime,
                endTime,
                duration,
                testCase,
                statusCode,
                exception
            );

            // Publish the notification to the VS Test Platform
            await _mediator.Publish(frameworkNotification, cancellationToken).ConfigureAwait(false);

            _logger.Log(LogLevel.Debug,
                "Published framework notification for timed-out test case '{0}' with status '{1}'",
                message.TestCaseId, statusCode);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to publish framework notification for test case '{0}': {1}",
                message.TestCaseId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Extracts the TestCase object from the message metadata.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The TestCase object.</returns>
    private static Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase ExtractTestCase(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue("TestCase", out var testCaseObj) &&
            testCaseObj is Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase testCase)
        {
            return testCase;
        }

        throw new InvalidOperationException($"TestCase not found in message metadata for test case '{message.TestCaseId}'");
    }

    /// <summary>
    /// Extracts the test output message from the message metadata.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The test output message string.</returns>
    private static string ExtractTestOutputMessage(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue("FormattedMessage", out var outputObj) &&
            outputObj is string outputMessage)
        {
            return outputMessage;
        }

        return string.Empty; // Default to empty string if not found
    }

    /// <summary>
    /// Extracts the start time from the message metadata.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The test start time.</returns>
    private static DateTimeOffset ExtractStartTime(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue("StartTime", out var startTimeObj) &&
            startTimeObj is DateTimeOffset startTime)
        {
            return startTime;
        }

        // Default to message completion time if start time not found
        return message.CompletedAt;
    }

    /// <summary>
    /// Extracts the end time from the message metadata.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The test end time.</returns>
    private static DateTimeOffset ExtractEndTime(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue("EndTime", out var endTimeObj) &&
            endTimeObj is DateTimeOffset endTime)
        {
            return endTime;
        }

        // Default to message completion time if end time not found
        return message.CompletedAt;
    }

    /// <summary>
    /// Calculates the test execution duration in milliseconds.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <param name="startTime">The test start time.</param>
    /// <param name="endTime">The test end time.</param>
    /// <returns>The test execution duration in milliseconds.</returns>
    private static double CalculateDuration(TestCompletionQueueMessage message, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        // Prefer the median from performance metrics if available
        if (message.PerformanceMetrics.MedianMs > 0)
        {
            return message.PerformanceMetrics.MedianMs;
        }

        // Fallback to time difference calculation
        var duration = (endTime - startTime).TotalMilliseconds;
        return Math.Max(0, duration); // Ensure non-negative duration
    }

    /// <summary>
    /// Determines the test status code from the message.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The test status code.</returns>
    private static StatusCode DetermineStatusCode(TestCompletionQueueMessage message)
    {
        // Use the test result from the message to determine status
        return message.TestResult.IsSuccess ? StatusCode.Success : StatusCode.Failure;
    }

    /// <summary>
    /// Extracts the exception from the message metadata.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The exception if present, null otherwise.</returns>
    private static Exception? ExtractException(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue("Exception", out var exceptionObj) &&
            exceptionObj is Exception exception)
        {
            return exception;
        }

        return null;
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the handler has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(BatchTimeoutHandler));
        }
    }

    /// <summary>
    /// Disposes the BatchTimeoutHandler and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the BatchTimeoutHandler and releases resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
        {
            lock (_lockObject)
            {
                _isRunning = false;
                _isDisposed = true;
            }

            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
        }
    }
}
