using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for processing test completion messages from the queue system.
/// This interface is part of the intercepting queue architecture that enables
/// asynchronous processing, batch analysis, and enhanced result generation before
/// test results are reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// Queue processors are responsible for consuming test completion messages from
/// the queue and performing various operations such as cross-test-case analysis,
/// batch processing, comparison analysis, and framework publishing. The intercepting
/// architecture ensures that all test results pass through queue processors before
/// reaching the VS Test Platform, enabling powerful batch processing capabilities.
/// 
/// Implementations of this interface should be thread-safe and handle high-throughput
/// scenarios where multiple test cases complete simultaneously. Processors may be
/// executed in parallel or in sequence depending on the processor pipeline configuration.
/// </remarks>
public interface ITestCompletionQueueProcessor
{
    /// <summary>
    /// Processes a test completion message from the queue system.
    /// </summary>
    /// <param name="message">
    /// The test completion message containing all relevant test execution data,
    /// including performance metrics, test results, and metadata. This message
    /// was published by the TestCaseCompletedNotificationHandler as part of the
    /// intercepting queue architecture.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the processing operation.
    /// The operation should be cancelled gracefully, ensuring proper cleanup
    /// and resource disposal.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous processing operation.
    /// The task completes when the message has been successfully processed.
    /// Processors may publish additional notifications or enhanced results
    /// as part of their processing logic.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="message"/> is null.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is called by the queue consumer service when messages are
    /// available for processing. Implementations should:
    /// 
    /// - Be thread-safe and handle concurrent execution
    /// - Process messages efficiently to avoid queue backlog
    /// - Handle errors gracefully without stopping the queue processing
    /// - Use the cancellation token to support graceful shutdown
    /// - Consider batching and cross-test-case analysis requirements
    /// 
    /// Common processor implementations include:
    /// - Framework Publishing Processor: Publishes FrameworkTestCaseEndNotification to VS Test Platform
    /// - Test Case Comparison Processor: Performs cross-test-case performance comparisons
    /// - Batch Completion Processor: Detects when test case batches are complete
    /// - Historical Data Processor: Stores test results for trend analysis
    /// - Report Generation Processor: Creates automated test reports
    /// - Alerting Processor: Sends notifications for test failures or performance issues
    /// </remarks>
    Task ProcessTestCompletion(TestCompletionQueueMessage message, CancellationToken cancellationToken);
}
