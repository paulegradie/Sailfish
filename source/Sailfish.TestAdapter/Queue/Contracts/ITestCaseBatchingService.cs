using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for the test case batching service that groups and manages related test cases
/// for batch processing and cross-test-case analysis. This service is a core component of the
/// intercepting queue architecture that enables powerful comparison and analysis capabilities
/// before test results are reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// The ITestCaseBatchingService is responsible for intelligently grouping test cases into batches
/// based on various strategies such as test class, comparison attributes, or custom criteria.
/// This batching enables cross-test-case analysis, performance comparison, and enhanced result
/// generation before framework publishing.
/// 
/// Key responsibilities:
/// - Group incoming test cases into logical batches for processing
/// - Detect when batches are complete and ready for analysis
/// - Support multiple batching strategies (by class, by attribute, custom)
/// - Manage batch lifecycle and status tracking
/// - Provide thread-safe operations for concurrent test execution
/// - Enable timeout handling for incomplete batches
/// 
/// The service integrates with the queue system to receive test completion messages and
/// organize them into batches that can be processed by queue processors for comparison
/// analysis, statistical evaluation, and enhanced result generation.
/// 
/// Batching strategies supported:
/// - ByTestClass: Group test cases by their containing test class
/// - ByComparisonAttribute: Group test cases marked with comparison attributes
/// - ByCustomCriteria: Group test cases using custom grouping logic
/// - ByExecutionContext: Group test cases by execution environment or settings
/// 
/// Thread safety: All operations must be thread-safe to support concurrent test execution
/// where multiple test cases may complete simultaneously and need to be added to batches.
/// </remarks>
public interface ITestCaseBatchingService
{
    #region Core Batching Operations

    /// <summary>
    /// Adds a test case to the appropriate batch based on the current batching strategy.
    /// The service will determine which batch the test case belongs to and add it accordingly.
    /// </summary>
    /// <param name="message">The test completion message to add to a batch.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous add operation. The task result contains
    /// the batch ID that the test case was added to.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the batching service is not started or has been stopped.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method analyzes the test completion message and determines the appropriate batch
    /// based on the current batching strategy. If no suitable batch exists, a new batch
    /// will be created. The method is thread-safe and can be called concurrently.
    /// </remarks>
    Task<string> AddTestCaseToBatchAsync(TestCompletionQueueMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified batch is complete and ready for processing.
    /// A batch is considered complete when all expected test cases have been received
    /// based on the batch completion criteria.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous check operation. The task result is true
    /// if the batch is complete, false otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="batchId"/> is null or empty.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified batch ID does not exist.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// Batch completion is determined by various criteria such as expected test count,
    /// timeout periods, or explicit completion signals. The method considers the
    /// batching strategy and completion criteria configured for the service.
    /// </remarks>
    Task<bool> IsBatchCompleteAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all batches that are complete and ready for processing by queue processors.
    /// This method returns batches that have met their completion criteria and can be
    /// processed for cross-test-case analysis and framework publishing.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous retrieval operation. The task result contains
    /// a collection of completed test case batches ready for processing.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is typically called by queue processors to retrieve batches for
    /// analysis and processing. Once a batch is retrieved and processed, it should
    /// be removed using RemoveBatchAsync to prevent memory leaks.
    /// </remarks>
    Task<IReadOnlyCollection<TestCaseBatch>> GetCompletedBatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific batch by its unique identifier.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous retrieval operation. The task result contains
    /// the test case batch if found, or null if the batch does not exist.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="batchId"/> is null or empty.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides access to individual batches for monitoring, analysis,
    /// or processing purposes. The returned batch includes all test cases currently
    /// assigned to it along with batch metadata and status information.
    /// </remarks>
    Task<TestCaseBatch?> GetBatchAsync(string batchId, CancellationToken cancellationToken = default);

    #endregion

    #region Batch Management Operations

    /// <summary>
    /// Retrieves all current batches managed by the batching service, including both
    /// complete and incomplete batches.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous retrieval operation. The task result contains
    /// a collection of all current test case batches.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is useful for monitoring and diagnostics to understand the current
    /// state of all batches. It includes batches in all states: pending, complete,
    /// processing, and timed out.
    /// </remarks>
    Task<IReadOnlyCollection<TestCaseBatch>> GetAllBatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a processed batch from the batching service to free up memory and resources.
    /// This method should be called after a batch has been successfully processed by
    /// queue processors.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch to remove.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous removal operation. The task result is true
    /// if the batch was successfully removed, false if the batch was not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="batchId"/> is null or empty.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is essential for memory management to prevent accumulation of
    /// processed batches. It should be called by queue processors after successful
    /// batch processing and framework publishing.
    /// </remarks>
    Task<bool> RemoveBatchAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a specific batch.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous status check operation. The task result contains
    /// the current status of the specified batch.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="batchId"/> is null or empty.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified batch ID does not exist.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides detailed status information about a batch including its
    /// current state, completion progress, and any timeout information.
    /// </remarks>
    Task<BatchStatus> GetBatchStatusAsync(string batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all pending batches that are currently waiting for completion.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a collection of TestCaseBatch objects that have a status of Pending.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides access to all batches that are currently pending completion,
    /// which is useful for timeout monitoring and batch processing scenarios. The method
    /// returns a snapshot of pending batches at the time of the call and is thread-safe.
    ///
    /// Pending batches are those with a status of BatchStatus.Pending and are actively
    /// waiting for additional test cases or timeout completion. This method is primarily
    /// used by timeout handlers and monitoring services to identify batches that may
    /// need timeout processing.
    /// </remarks>
    Task<IEnumerable<TestCaseBatch>> GetPendingBatchesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Batching Strategy Configuration

    /// <summary>
    /// Sets the batching strategy used to group test cases into batches.
    /// </summary>
    /// <param name="strategy">The batching strategy to use.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous strategy configuration operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="strategy"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the strategy cannot be changed due to active batches or service state.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// Changing the batching strategy may affect how new test cases are grouped.
    /// Existing batches will continue to use their original strategy until completed.
    /// </remarks>
    Task SetBatchingStrategyAsync(BatchingStrategy strategy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current batching strategy being used to group test cases.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous strategy retrieval operation. The task result
    /// contains the current batching strategy.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    Task<BatchingStrategy> GetBatchingStrategyAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// Starts the batching service and initializes it for receiving test cases.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the service is already started or in an invalid state.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method must be called before adding test cases to batches. It initializes
    /// internal data structures and prepares the service for batch management.
    /// </remarks>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the batching service and completes all pending batches.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method gracefully shuts down the batching service, marking all incomplete
    /// batches as complete and ensuring they are available for processing. It should
    /// be called during test execution cleanup.
    /// </remarks>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the batching service as complete, indicating that no more test cases will be added.
    /// This triggers completion of all pending batches based on current contents.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous completion operation.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is called when test execution is complete and no more test cases
    /// are expected. It ensures that all batches are marked as complete and ready
    /// for final processing, even if they haven't reached their expected size.
    /// </remarks>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Represents a batch of related test cases grouped together for processing and analysis.
/// </summary>
/// <remarks>
/// A TestCaseBatch contains a collection of test completion messages that have been
/// grouped together based on the batching strategy. The batch includes metadata
/// about its creation, completion criteria, and current status.
/// </remarks>
public class TestCaseBatch
{
    /// <summary>
    /// Gets or sets the unique identifier for this batch.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of test completion messages in this batch.
    /// </summary>
    public List<TestCompletionQueueMessage> TestCases { get; set; } = new();

    /// <summary>
    /// Gets or sets the current status of this batch.
    /// </summary>
    public BatchStatus Status { get; set; } = BatchStatus.Pending;

    /// <summary>
    /// Gets or sets the timestamp when this batch was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this batch was completed.
    /// Null if the batch is not yet complete.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the batching strategy used to create this batch.
    /// </summary>
    public BatchingStrategy Strategy { get; set; }

    /// <summary>
    /// Gets or sets the grouping criteria used to determine batch membership.
    /// This could be a test class name, comparison group identifier, or custom criteria.
    /// </summary>
    public string GroupingCriteria { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected number of test cases for this batch.
    /// Null if the expected count is not known or not applicable.
    /// </summary>
    public int? ExpectedTestCaseCount { get; set; }

    /// <summary>
    /// Gets or sets the timeout for batch completion.
    /// If test cases are not received within this timeout, the batch will be marked as complete.
    /// </summary>
    public TimeSpan? CompletionTimeout { get; set; }

    /// <summary>
    /// Gets or sets additional metadata associated with this batch.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the current status of a test case batch.
/// </summary>
public enum BatchStatus
{
    /// <summary>
    /// The batch is pending and waiting for more test cases.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The batch is complete and ready for processing.
    /// </summary>
    Complete = 1,

    /// <summary>
    /// The batch is currently being processed by queue processors.
    /// </summary>
    Processing = 2,

    /// <summary>
    /// The batch has been processed and published to the framework.
    /// </summary>
    Processed = 3,

    /// <summary>
    /// The batch timed out before receiving all expected test cases.
    /// </summary>
    TimedOut = 4,

    /// <summary>
    /// The batch encountered an error during processing.
    /// </summary>
    Error = 5
}

/// <summary>
/// Defines the strategy used for grouping test cases into batches.
/// </summary>
public enum BatchingStrategy
{
    /// <summary>
    /// Group test cases by their containing test class.
    /// All test methods from the same test class will be grouped together.
    /// </summary>
    ByTestClass = 0,

    /// <summary>
    /// Group test cases by comparison attributes or markers.
    /// Test cases marked with the same comparison group will be batched together.
    /// </summary>
    ByComparisonAttribute = 1,

    /// <summary>
    /// Group test cases using custom criteria defined in metadata.
    /// The grouping logic is determined by custom metadata values.
    /// </summary>
    ByCustomCriteria = 2,

    /// <summary>
    /// Group test cases by their execution context or environment.
    /// Test cases with similar execution settings will be grouped together.
    /// </summary>
    ByExecutionContext = 3,

    /// <summary>
    /// Group test cases by performance characteristics or thresholds.
    /// Test cases with similar performance profiles will be batched together.
    /// </summary>
    ByPerformanceProfile = 4,

    /// <summary>
    /// No batching - each test case is processed individually.
    /// This effectively disables batch processing.
    /// </summary>
    None = 5
}
