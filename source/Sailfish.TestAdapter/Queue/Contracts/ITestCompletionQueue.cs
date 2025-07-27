using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for the core queue service that manages test completion messages
/// in the intercepting queue architecture. This interface provides the fundamental
/// queue operations for enqueuing, dequeuing, and managing the lifecycle of test
/// completion messages before they are processed and reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// The ITestCompletionQueue is the central component of the intercepting queue architecture
/// that enables asynchronous processing, batch analysis, and cross-test-case comparison
/// before test results reach the VS Test Platform. This queue sits between the
/// TestCaseCompletedNotificationHandler (which publishes messages) and the queue
/// processors (which consume and process messages).
/// 
/// Key architectural responsibilities:
/// - Accept test completion messages from the queue publisher
/// - Provide messages to the queue consumer service for processing
/// - Manage queue lifecycle during test execution
/// - Support graceful shutdown and completion detection
/// - Provide monitoring and status information for queue health
/// 
/// The queue implementation should be thread-safe and support high-throughput
/// scenarios where multiple test cases complete simultaneously. The queue exists
/// only during test execution and does not persist data across test runs.
/// </remarks>
public interface ITestCompletionQueue
{
    /// <summary>
    /// Gets a value indicating whether the queue service is currently running
    /// and accepting new messages.
    /// </summary>
    /// <value>
    /// <c>true</c> if the queue is running and operational; otherwise, <c>false</c>.
    /// </value>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the current number of messages waiting in the queue for processing.
    /// </summary>
    /// <value>
    /// The number of test completion messages currently queued for processing.
    /// </value>
    int QueueDepth { get; }

    /// <summary>
    /// Gets a value indicating whether the queue has been marked as complete
    /// and will not accept any new messages.
    /// </summary>
    /// <value>
    /// <c>true</c> if the queue is complete and no more messages will be added;
    /// otherwise, <c>false</c>.
    /// </value>
    bool IsCompleted { get; }

    /// <summary>
    /// Starts the queue service and prepares it to accept and process messages.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the start operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous start operation.
    /// The task completes when the queue is ready to accept messages.
    /// </returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the queue is already running or has been completed.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method should be called during test execution setup to initialize
    /// the queue before any test completion messages are published. The queue
    /// must be started before it can accept messages via EnqueueAsync.
    /// </remarks>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the queue service gracefully, allowing existing messages to be processed
    /// but preventing new messages from being accepted.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the stop operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous stop operation.
    /// The task completes when the queue has been stopped gracefully.
    /// </returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method should be called during test execution cleanup to ensure
    /// all queued messages are processed before the test execution completes.
    /// After stopping, the queue will not accept new messages but will continue
    /// to provide existing messages to consumers until the queue is empty.
    /// </remarks>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks the queue as complete, indicating that no more messages will be added.
    /// This allows consumers to detect when all expected messages have been received.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the complete operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous complete operation.
    /// The task completes when the queue has been marked as complete.
    /// </returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is typically called when all test cases have completed execution
    /// and no more test completion messages will be published. Queue consumers
    /// can use the IsCompleted property to detect when all messages have been
    /// processed and batch completion can be finalized.
    /// </remarks>
    Task CompleteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Adds a test completion message to the queue for asynchronous processing.
    /// </summary>
    /// <param name="message">
    /// The test completion message containing all relevant test execution data,
    /// including performance metrics, test results, and metadata required for
    /// queue processing and cross-test-case analysis.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the enqueue operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous enqueue operation.
    /// The task completes when the message has been successfully added to the queue.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="message"/> is null.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the queue is not running or has been completed.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is called by the queue publisher service when test completion
    /// messages are received from the TestCaseCompletedNotificationHandler.
    /// The message will be made available to queue consumers for processing
    /// by registered queue processors.
    /// 
    /// The implementation should be thread-safe and handle high-throughput
    /// scenarios where multiple test cases complete simultaneously.
    /// </remarks>
    Task EnqueueAsync(TestCompletionQueueMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Removes and returns a test completion message from the queue.
    /// This method will wait until a message is available or the queue is completed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the dequeue operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous dequeue operation.
    /// The task result contains the test completion message that was removed from the queue,
    /// or null if the queue is completed and no more messages are available.
    /// </returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is called by the queue consumer service to retrieve messages
    /// for processing by queue processors. The method will block until a message
    /// is available or the queue is marked as complete.
    /// 
    /// If the queue is completed and no messages remain, this method returns null
    /// to indicate that no more messages will be available.
    /// </remarks>
    Task<TestCompletionQueueMessage?> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to remove and return a test completion message from the queue
    /// without waiting. Returns immediately with null if no message is available.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous try-dequeue operation.
    /// The task result contains the test completion message that was removed from the queue,
    /// or null if no message was immediately available.
    /// </returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides a non-blocking alternative to DequeueAsync for scenarios
    /// where the consumer wants to check for available messages without waiting.
    /// This can be useful for implementing polling-based consumption patterns
    /// or for queue monitoring and health check scenarios.
    /// </remarks>
    Task<TestCompletionQueueMessage?> TryDequeueAsync(CancellationToken cancellationToken);
}
