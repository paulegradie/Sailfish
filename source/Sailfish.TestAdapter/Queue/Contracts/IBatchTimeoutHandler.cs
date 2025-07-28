using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for a service that monitors and handles timeout scenarios for incomplete test case batches.
/// This interface is part of the intercepting queue architecture that enables batch processing and cross-test-case
/// analysis before test results are reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// The IBatchTimeoutHandler is responsible for monitoring batches that have exceeded their completion timeout
/// and processing them to ensure test results are eventually reported to the VS Test Platform. This prevents
/// tests from hanging indefinitely when batches don't complete naturally due to missing test cases or other issues.
/// 
/// Key responsibilities:
/// - Monitor batch completion timeouts using configurable timeout values
/// - Detect batches that have exceeded their completion timeout
/// - Process incomplete batches by publishing framework notifications for available test cases
/// - Update batch status to TimedOut for proper tracking
/// - Provide comprehensive logging and error reporting for timeout scenarios
/// - Support different timeout values per batch type and strategy
/// 
/// The timeout handler operates as a background service that periodically checks for timed-out batches
/// and processes them to maintain the flow of test results to the VS Test Platform. It integrates with
/// the existing batching service and framework publishing mechanisms to ensure seamless operation.
/// 
/// Thread Safety:
/// All operations must be thread-safe to support concurrent test execution scenarios where multiple
/// test cases may complete simultaneously and batches may timeout concurrently.
/// </remarks>
public interface IBatchTimeoutHandler
{
    /// <summary>
    /// Starts the batch timeout monitoring service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the startup operation.</param>
    /// <returns>A task that represents the asynchronous startup operation.</returns>
    /// <remarks>
    /// This method initializes the timeout monitoring mechanism and begins periodic checking
    /// for timed-out batches. The service will continue monitoring until StopAsync is called.
    /// The method should be called during queue system startup.
    /// </remarks>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the batch timeout monitoring service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the shutdown operation.</param>
    /// <returns>A task that represents the asynchronous shutdown operation.</returns>
    /// <remarks>
    /// This method stops the timeout monitoring mechanism and ensures all pending timeout
    /// processing operations are completed before returning. The method should be called
    /// during queue system shutdown to ensure proper cleanup.
    /// </remarks>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually triggers a check for timed-out batches and processes any that are found.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous timeout processing operation. The task result
    /// contains the number of batches that were processed due to timeout.
    /// </returns>
    /// <remarks>
    /// This method provides a way to manually trigger timeout processing, which is useful
    /// for testing scenarios or when immediate timeout processing is required. It performs
    /// the same timeout detection and processing logic as the periodic monitoring.
    /// </remarks>
    Task<int> ProcessTimedOutBatchesAsync(CancellationToken cancellationToken = default);
}
