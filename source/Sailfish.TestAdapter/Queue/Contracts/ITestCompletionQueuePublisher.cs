using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for publishing test completion messages to the queue system.
/// This interface is part of the intercepting queue architecture that enables
/// asynchronous processing and batch analysis of test completion events before
/// they are reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// The queue publisher is responsible for accepting test completion messages
/// from notification handlers and ensuring they are properly queued for
/// processing by the queue processors. This enables cross-test-case analysis,
/// batching, and enhanced result generation before framework reporting.
/// </remarks>
public interface ITestCompletionQueuePublisher
{
    /// <summary>
    /// Publishes a test completion message to the queue for asynchronous processing.
    /// </summary>
    /// <param name="message">
    /// The test completion message containing all relevant test execution data,
    /// including performance metrics, test results, and metadata required for
    /// queue processing and cross-test-case analysis.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the publishing operation.
    /// The operation should be cancelled gracefully, ensuring no data loss.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous publishing operation.
    /// The task completes when the message has been successfully queued
    /// for processing by the queue processors.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="message"/> is null.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is called by the TestCaseCompletedNotificationHandler as part
    /// of the intercepting queue architecture. The message will be processed by
    /// registered queue processors, which may include batching services,
    /// comparison analyzers, and framework publishing processors.
    /// 
    /// The implementation should be thread-safe and handle high-throughput
    /// scenarios where multiple test cases complete simultaneously.
    /// </remarks>
    Task PublishTestCompletion(TestCompletionQueueMessage message, CancellationToken cancellationToken);
}
