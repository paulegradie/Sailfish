using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Implementation;

/// <summary>
/// Implementation of the queue publisher service that publishes test completion messages
/// to the queue system for asynchronous processing. This class is a core component of the
/// intercepting queue architecture that enables batch processing and cross-test-case analysis
/// before test results are reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// The TestCompletionQueuePublisher serves as the bridge between the test completion
/// notification handlers and the queue system. When test cases complete execution,
/// the TestCaseCompletedNotificationHandler uses this publisher to send test completion
/// messages to the queue instead of directly publishing to the framework.
/// 
/// Key responsibilities:
/// - Accept test completion messages from notification handlers
/// - Validate message data and parameters
/// - Delegate message publishing to the underlying queue implementation
/// - Provide proper error handling and logging for queue publishing failures
/// - Support high-throughput scenarios with thread-safe operations
/// 
/// The publisher is designed to be lightweight and efficient, focusing on message
/// validation and delegation rather than complex processing logic. The actual queue
/// management and message processing is handled by the ITestCompletionQueue implementation
/// and associated queue processors.
/// 
/// This implementation supports the intercepting queue architecture where:
/// 1. TestCaseCompletedNotificationHandler publishes to queue (via this publisher)
/// 2. Queue processors consume messages and perform batch analysis
/// 3. Framework publishing processor sends enhanced results to VS Test Platform
/// 
/// Thread Safety:
/// This class is thread-safe and can handle concurrent publishing requests from
/// multiple test cases completing simultaneously. The thread safety is achieved
/// through delegation to the thread-safe ITestCompletionQueue implementation.
/// </remarks>
public class TestCompletionQueuePublisher : ITestCompletionQueuePublisher
{
    private readonly ITestCompletionQueue _queue;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCompletionQueuePublisher"/> class.
    /// </summary>
    /// <param name="queue">
    /// The test completion queue service that will receive and manage published messages.
    /// This queue handles the actual message storage and provides messages to queue consumers.
    /// </param>
    /// <param name="logger">
    /// The logger service for recording queue publishing operations, errors, and diagnostic information.
    /// Used to log publishing failures and other significant events for troubleshooting.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> or <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// The publisher requires both a queue service for message delegation and a logger
    /// for error reporting. These dependencies are typically injected by the DI container
    /// during test adapter initialization.
    /// </remarks>
    public TestCompletionQueuePublisher(ITestCompletionQueue queue, ILogger logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishTestCompletion(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        try
        {
            // Delegate to the queue implementation for actual message publishing
            await _queue.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            // Queue state issues (not running, completed, etc.)
            _logger.Log(LogLevel.Error, 
                $"Failed to publish test completion message for test case '{message.TestCaseId}': Queue is in invalid state. {ex.Message}");
            throw;
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected behavior, log as warning
            _logger.Log(LogLevel.Warning, 
                $"Publishing test completion message for test case '{message.TestCaseId}' was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected errors should be logged and re-thrown
            _logger.Log(LogLevel.Error, 
                $"Unexpected error publishing test completion message for test case '{message.TestCaseId}': {ex.Message}");
            throw;
        }
    }
}
