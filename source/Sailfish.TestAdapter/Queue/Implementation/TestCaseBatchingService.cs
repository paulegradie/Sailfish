using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Implementation;

/// <summary>
/// Implementation of the test case batching service that groups and manages related test cases
/// for batch processing and cross-test-case analysis. This service is a core component of the
/// intercepting queue architecture that enables powerful comparison and analysis capabilities
/// before test results are reported to the VS Test Platform.
/// </summary>
/// <remarks>
/// The TestCaseBatchingService provides intelligent grouping of test cases into batches based on
/// various strategies such as test class, comparison attributes, or custom criteria. This batching
/// enables cross-test-case analysis, performance comparison, and enhanced result generation
/// before framework publishing.
/// 
/// Key features:
/// - Thread-safe batch management using concurrent collections
/// - Multiple batching strategies (by class, by attribute, custom criteria)
/// - Batch completion detection with timeout handling
/// - Memory-efficient storage and lifecycle management
/// - Integration with Sailfish logging infrastructure
/// - Support for dynamic batch sizing based on test discovery
/// - Graceful startup and shutdown with proper resource cleanup
/// 
/// The service maintains batches in memory during test execution and provides methods for
/// adding test cases, checking completion status, and retrieving completed batches for
/// processing by queue processors.
/// 
/// Thread Safety:
/// All operations are thread-safe and designed to handle concurrent test execution scenarios
/// where multiple test cases may complete simultaneously and need to be added to batches.
/// </remarks>
public class TestCaseBatchingService : ITestCaseBatchingService, IDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, TestCaseBatch> _batches;
    private readonly object _strategyLock = new();
    
    private BatchingStrategy _currentStrategy = BatchingStrategy.ByTestClass;
    private bool _isStarted = false;
    private bool _isCompleted = false;
    private bool _isDisposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCaseBatchingService"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger service for recording batching operations and diagnostic information.
    /// This logger integrates with the Sailfish logging infrastructure.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// The service is initialized with the ByTestClass batching strategy as the default.
    /// Call StartAsync before adding test cases to batches.
    /// </remarks>
    public TestCaseBatchingService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _batches = new ConcurrentDictionary<string, TestCaseBatch>();
        
        _logger.Log(LogLevel.Debug, "TestCaseBatchingService initialized with default ByTestClass strategy");
    }

    #region Core Batching Operations

    /// <inheritdoc />
    public async Task<string> AddTestCaseToBatchAsync(TestCompletionQueueMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        ThrowIfDisposed();
        
        if (!_isStarted)
        {
            throw new InvalidOperationException("Batching service is not started. Call StartAsync before adding test cases.");
        }

        if (_isCompleted)
        {
            throw new InvalidOperationException("Batching service has been completed and cannot accept new test cases.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Determine the batch ID based on the current strategy
            var batchId = await DetermineBatchId(message, cancellationToken).ConfigureAwait(false);
            
            // Get or create the batch
            var batch = _batches.GetOrAdd(batchId, id => CreateNewBatch(id, message));
            
            // Add the test case to the batch in a thread-safe manner
            lock (batch)
            {
                batch.TestCases.Add(message);
                
                _logger.Log(LogLevel.Debug, 
                    "Added test case '{0}' to batch '{1}'. Batch now contains {2} test cases.",
                    message.TestCaseId, batchId, batch.TestCases.Count);
            }

            return batchId;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex, 
                "Failed to add test case '{0}' to batch: {1}", 
                message.TestCaseId, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsBatchCompleteAsync(string batchId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(batchId))
        {
            throw new ArgumentNullException(nameof(batchId));
        }

        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (!_batches.TryGetValue(batchId, out var batch))
        {
            throw new ArgumentException($"Batch with ID '{batchId}' does not exist.", nameof(batchId));
        }

        return await Task.FromResult(IsBatchComplete(batch)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TestCaseBatch>> GetCompletedBatchesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        var completedBatches = new List<TestCaseBatch>();

        foreach (var kvp in _batches)
        {
            var batch = kvp.Value;
            lock (batch)
            {
                if (IsBatchComplete(batch) && batch.Status != BatchStatus.Processing && batch.Status != BatchStatus.Processed)
                {
                    // Mark as complete if not already marked
                    if (batch.Status == BatchStatus.Pending)
                    {
                        batch.Status = BatchStatus.Complete;
                        batch.CompletedAt = DateTime.UtcNow;
                        
                        _logger.Log(LogLevel.Information, 
                            "Batch '{0}' marked as complete with {1} test cases", 
                            batch.BatchId, batch.TestCases.Count);
                    }
                    
                    completedBatches.Add(batch);
                }
            }
        }

        return await Task.FromResult<IReadOnlyCollection<TestCaseBatch>>(completedBatches).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TestCaseBatch?> GetBatchAsync(string batchId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(batchId))
        {
            throw new ArgumentNullException(nameof(batchId));
        }

        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        _batches.TryGetValue(batchId, out var batch);
        return await Task.FromResult(batch).ConfigureAwait(false);
    }

    #endregion

    #region Batch Management Operations

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TestCaseBatch>> GetAllBatchesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        var allBatches = _batches.Values.ToList();
        return await Task.FromResult<IReadOnlyCollection<TestCaseBatch>>(allBatches).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveBatchAsync(string batchId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(batchId))
        {
            throw new ArgumentNullException(nameof(batchId));
        }

        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        var removed = _batches.TryRemove(batchId, out var batch);
        
        if (removed && batch != null)
        {
            _logger.Log(LogLevel.Debug, 
                "Removed batch '{0}' containing {1} test cases from batching service", 
                batchId, batch.TestCases.Count);
        }

        return await Task.FromResult(removed).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BatchStatus> GetBatchStatusAsync(string batchId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(batchId))
        {
            throw new ArgumentNullException(nameof(batchId));
        }

        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (!_batches.TryGetValue(batchId, out var batch))
        {
            throw new ArgumentException($"Batch with ID '{batchId}' does not exist.", nameof(batchId));
        }

        BatchStatus status;
        lock (batch)
        {
            status = batch.Status;
        }

        return await Task.FromResult(status).ConfigureAwait(false);
    }

    #endregion

    #region Batching Strategy Configuration

    /// <inheritdoc />
    public async Task SetBatchingStrategyAsync(BatchingStrategy strategy, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        lock (_strategyLock)
        {
            if (_batches.Count > 0)
            {
                throw new InvalidOperationException(
                    "Cannot change batching strategy while active batches exist. " +
                    "Complete or remove all batches before changing strategy.");
            }

            _currentStrategy = strategy;
            
            _logger.Log(LogLevel.Information, 
                "Batching strategy changed to '{0}'", strategy);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BatchingStrategy> GetBatchingStrategyAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        BatchingStrategy strategy;
        lock (_strategyLock)
        {
            strategy = _currentStrategy;
        }

        return await Task.FromResult(strategy).ConfigureAwait(false);
    }

    #endregion

    #region Lifecycle Management

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (_isStarted)
        {
            throw new InvalidOperationException("Batching service is already started.");
        }

        _isStarted = true;
        _isCompleted = false;
        
        _logger.Log(LogLevel.Information, 
            "TestCaseBatchingService started with strategy '{0}'", _currentStrategy);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (!_isStarted)
        {
            return; // Already stopped or never started
        }

        // Mark all pending batches as complete
        await CompleteAllPendingBatches(cancellationToken).ConfigureAwait(false);

        _isStarted = false;
        
        _logger.Log(LogLevel.Information, 
            "TestCaseBatchingService stopped. Total batches managed: {0}", _batches.Count);
    }

    /// <inheritdoc />
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        _isCompleted = true;
        
        // Mark all pending batches as complete since no more test cases will be added
        await CompleteAllPendingBatches(cancellationToken).ConfigureAwait(false);
        
        _logger.Log(LogLevel.Information, 
            "TestCaseBatchingService marked as complete. All pending batches have been finalized.");
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Determines the batch ID for a test completion message based on the current batching strategy.
    /// </summary>
    /// <param name="message">The test completion message to determine batch ID for.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The batch ID that the test case should be added to.</returns>
    private async Task<string> DetermineBatchId(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _currentStrategy switch
        {
            BatchingStrategy.ByTestClass => await DetermineBatchIdByTestClass(message, cancellationToken).ConfigureAwait(false),
            BatchingStrategy.ByComparisonAttribute => await DetermineBatchIdByComparisonAttribute(message, cancellationToken).ConfigureAwait(false),
            BatchingStrategy.ByCustomCriteria => await DetermineBatchIdByCustomCriteria(message, cancellationToken).ConfigureAwait(false),
            BatchingStrategy.ByExecutionContext => await DetermineBatchIdByExecutionContext(message, cancellationToken).ConfigureAwait(false),
            BatchingStrategy.ByPerformanceProfile => await DetermineBatchIdByPerformanceProfile(message, cancellationToken).ConfigureAwait(false),
            BatchingStrategy.None => await DetermineBatchIdForNoBatching(message, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Unsupported batching strategy: {_currentStrategy}")
        };
    }

    /// <summary>
    /// Determines batch ID by test class name.
    /// </summary>
    private async Task<string> DetermineBatchIdByTestClass(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Extract test class name from test case ID or metadata
        var testClassName = ExtractTestClassName(message);
        return await Task.FromResult($"TestClass_{testClassName}").ConfigureAwait(false);
    }

    /// <summary>
    /// Determines batch ID by comparison attribute or grouping ID.
    /// </summary>
    private async Task<string> DetermineBatchIdByComparisonAttribute(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Use grouping ID from performance metrics if available
        var groupingId = message.PerformanceMetrics?.GroupingId;
        if (!string.IsNullOrEmpty(groupingId))
        {
            return await Task.FromResult($"Comparison_{groupingId}").ConfigureAwait(false);
        }

        // Fall back to test class if no grouping ID is available
        return await DetermineBatchIdByTestClass(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines batch ID by custom criteria from metadata.
    /// </summary>
    private async Task<string> DetermineBatchIdByCustomCriteria(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Look for custom batching criteria in metadata
        if (message.Metadata.TryGetValue("BatchingCriteria", out var criteria) && criteria != null)
        {
            return await Task.FromResult($"Custom_{criteria}").ConfigureAwait(false);
        }

        // Fall back to test class if no custom criteria is available
        return await DetermineBatchIdByTestClass(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines batch ID by execution context.
    /// </summary>
    private async Task<string> DetermineBatchIdByExecutionContext(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Create batch ID based on execution context metadata
        var contextKey = "Default";
        if (message.Metadata.TryGetValue("ExecutionContext", out var context) && context != null)
        {
            contextKey = context.ToString() ?? "Default";
        }

        return await Task.FromResult($"Context_{contextKey}").ConfigureAwait(false);
    }

    /// <summary>
    /// Determines batch ID by performance profile characteristics.
    /// </summary>
    private async Task<string> DetermineBatchIdByPerformanceProfile(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Group by performance characteristics (e.g., execution time ranges)
        var meanMs = message.PerformanceMetrics?.MeanMs ?? 0;
        var profileCategory = meanMs switch
        {
            < 10 => "Fast",
            < 100 => "Medium",
            < 1000 => "Slow",
            _ => "VerySlow"
        };

        return await Task.FromResult($"Performance_{profileCategory}").ConfigureAwait(false);
    }

    /// <summary>
    /// Determines batch ID for no batching strategy (each test case gets its own batch).
    /// </summary>
    private async Task<string> DetermineBatchIdForNoBatching(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Each test case gets its own unique batch
        return await Task.FromResult($"Individual_{message.TestCaseId}_{Guid.NewGuid():N}").ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts the test class name from a test completion message.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The test class name or a default value if not found.</returns>
    private string ExtractTestClassName(TestCompletionQueueMessage message)
    {
        // Try to extract class name from test case ID (format: ClassName.MethodName)
        var testCaseId = message.TestCaseId;
        if (!string.IsNullOrEmpty(testCaseId))
        {
            var lastDotIndex = testCaseId.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                return testCaseId.Substring(0, lastDotIndex);
            }
        }

        // Try to get class name from metadata
        if (message.Metadata.TryGetValue("TestClassName", out var className) && className != null)
        {
            return className.ToString() ?? "Unknown";
        }

        // Default fallback
        return "Unknown";
    }

    /// <summary>
    /// Creates a new batch for the specified batch ID and initial test case.
    /// </summary>
    /// <param name="batchId">The unique identifier for the new batch.</param>
    /// <param name="initialMessage">The first test case to add to the batch.</param>
    /// <returns>A new TestCaseBatch instance.</returns>
    private TestCaseBatch CreateNewBatch(string batchId, TestCompletionQueueMessage initialMessage)
    {
        var batch = new TestCaseBatch
        {
            BatchId = batchId,
            Status = BatchStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Strategy = _currentStrategy,
            GroupingCriteria = DetermineGroupingCriteria(initialMessage),
            CompletionTimeout = TimeSpan.FromMinutes(5), // Default 5-minute timeout
            Metadata = new Dictionary<string, object>
            {
                ["CreatedBy"] = nameof(TestCaseBatchingService),
                ["Strategy"] = _currentStrategy.ToString()
            }
        };

        _logger.Log(LogLevel.Debug,
            "Created new batch '{0}' with strategy '{1}' and grouping criteria '{2}'",
            batchId, _currentStrategy, batch.GroupingCriteria);

        return batch;
    }

    /// <summary>
    /// Determines the grouping criteria for a batch based on the test message and current strategy.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The grouping criteria string.</returns>
    private string DetermineGroupingCriteria(TestCompletionQueueMessage message)
    {
        return _currentStrategy switch
        {
            BatchingStrategy.ByTestClass => ExtractTestClassName(message),
            BatchingStrategy.ByComparisonAttribute => message.PerformanceMetrics?.GroupingId ?? ExtractTestClassName(message),
            BatchingStrategy.ByCustomCriteria => message.Metadata.TryGetValue("BatchingCriteria", out var criteria) ? criteria?.ToString() ?? "Default" : "Default",
            BatchingStrategy.ByExecutionContext => message.Metadata.TryGetValue("ExecutionContext", out var context) ? context?.ToString() ?? "Default" : "Default",
            BatchingStrategy.ByPerformanceProfile => DeterminePerformanceProfile(message.PerformanceMetrics?.MeanMs ?? 0),
            BatchingStrategy.None => message.TestCaseId,
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Determines the performance profile category based on mean execution time.
    /// </summary>
    /// <param name="meanMs">The mean execution time in milliseconds.</param>
    /// <returns>The performance profile category.</returns>
    private string DeterminePerformanceProfile(double meanMs)
    {
        return meanMs switch
        {
            < 10 => "Fast",
            < 100 => "Medium",
            < 1000 => "Slow",
            _ => "VerySlow"
        };
    }

    /// <summary>
    /// Determines if a batch is complete based on its current state and completion criteria.
    /// </summary>
    /// <param name="batch">The batch to check for completion.</param>
    /// <returns>True if the batch is complete, false otherwise.</returns>
    private bool IsBatchComplete(TestCaseBatch batch)
    {
        // If service is completed, all batches are considered complete
        if (_isCompleted)
        {
            return true;
        }

        // Check if batch has already been marked as complete or processed
        if (batch.Status == BatchStatus.Complete ||
            batch.Status == BatchStatus.Processing ||
            batch.Status == BatchStatus.Processed)
        {
            return true;
        }

        // Check if expected test case count is reached
        if (batch.ExpectedTestCaseCount.HasValue &&
            batch.TestCases.Count >= batch.ExpectedTestCaseCount.Value)
        {
            return true;
        }

        // Check if completion timeout has been reached
        if (batch.CompletionTimeout.HasValue)
        {
            var elapsed = DateTime.UtcNow - batch.CreatedAt;
            if (elapsed >= batch.CompletionTimeout.Value)
            {
                return true;
            }
        }

        // For None strategy, each test case is its own complete batch
        if (_currentStrategy == BatchingStrategy.None)
        {
            return batch.TestCases.Count > 0;
        }

        return false;
    }

    /// <summary>
    /// Marks all pending batches as complete.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CompleteAllPendingBatches(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var completedCount = 0;
        foreach (var kvp in _batches)
        {
            var batch = kvp.Value;
            lock (batch)
            {
                if (batch.Status == BatchStatus.Pending)
                {
                    batch.Status = BatchStatus.Complete;
                    batch.CompletedAt = DateTime.UtcNow;
                    completedCount++;
                }
            }
        }

        if (completedCount > 0)
        {
            _logger.Log(LogLevel.Information,
                "Marked {0} pending batches as complete during service completion", completedCount);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(TestCaseBatchingService));
        }
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Releases all resources used by the <see cref="TestCaseBatchingService"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="TestCaseBatchingService"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
        {
            try
            {
                // Stop the service if it's still running
                if (_isStarted)
                {
                    StopAsync(CancellationToken.None).GetAwaiter().GetResult();
                }

                // Clear all batches
                _batches.Clear();

                _logger.Log(LogLevel.Debug, "TestCaseBatchingService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex, "Error occurred during TestCaseBatchingService disposal: {0}", ex.Message);
            }
            finally
            {
                _isDisposed = true;
            }
        }
    }

    #endregion
}
