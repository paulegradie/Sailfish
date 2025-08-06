using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Notifications;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Defines the contract for mapping test case completion notifications to queue messages
/// with comprehensive metadata for batch processing and cross-test-case analysis.
/// </summary>
/// <remarks>
/// The ITestCompletionMessageMapper is responsible for transforming TestCaseCompletedNotification
/// objects into TestCompletionQueueMessage objects that contain all necessary data for
/// queue processing, batch analysis, and framework publishing.
/// 
/// Key responsibilities:
/// - Extract test execution results and performance metrics from notifications
/// - Generate formatted output messages for display in test explorers
/// - Include batching metadata for cross-test-case analysis and grouping
/// - Handle SailDiff analysis integration when enabled
/// - Provide comprehensive error handling for missing or invalid data
/// - Support thread-safe operations for concurrent test execution
/// 
/// The mapper extracts and includes metadata required for various batching strategies:
/// - Test class information for ByTestClass batching strategy
/// - Comparison group identifiers for ByComparisonAttribute batching
/// - Custom criteria metadata for ByCustomCriteria batching
/// - Execution context data for ByExecutionContext batching
/// - Performance profile data for ByPerformanceProfile batching
/// 
/// Thread Safety:
/// Implementations must be thread-safe to support concurrent test execution where
/// multiple test cases may complete simultaneously and require mapping to queue messages.
/// 
/// Integration Points:
/// - Integrates with existing Sailfish notification system
/// - Uses Sailfish formatting services for output generation
/// - Leverages SailDiff analysis for performance comparison
/// - Supports all existing test execution features and settings
/// </remarks>
public interface ITestCompletionMessageMapper
{
    /// <summary>
    /// Maps a test case completion notification to a queue message with comprehensive
    /// metadata for batch processing and cross-test-case analysis.
    /// </summary>
    /// <param name="notification">
    /// The test case completion notification containing execution results and performance data.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the mapping operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous mapping operation. The task result contains
    /// a TestCompletionQueueMessage with all test execution data and metadata required
    /// for queue processing and framework publishing.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="notification"/> is null.
    /// </exception>
    /// <exception cref="Sailfish.Exceptions.SailfishException">
    /// Thrown when required notification data is missing or invalid, such as when
    /// TestInstanceContainerExternal or PerformanceTimer is null.
    /// </exception>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method performs comprehensive data extraction and transformation:
    /// 
    /// 1. **Validation**: Validates notification data for required components
    /// 2. **Performance Analysis**: Extracts performance metrics and statistical data
    /// 3. **Result Processing**: Determines test success/failure status and exception details
    /// 4. **Output Formatting**: Generates formatted messages for test explorer display
    /// 5. **SailDiff Integration**: Performs performance comparison analysis when enabled
    /// 6. **Metadata Extraction**: Includes batching and grouping metadata for processors
    /// 7. **Framework Data**: Packages all data required for VS Test Platform publishing
    /// 
    /// The resulting TestCompletionQueueMessage contains:
    /// - Test case identification and execution results
    /// - Performance metrics with statistical analysis
    /// - Formatted output messages for display
    /// - Comprehensive metadata for batch processing
    /// - All data required for framework publishing processor
    /// 
    /// Batching Metadata Included:
    /// - Test class name and assembly information
    /// - Comparison group identifiers and attributes
    /// - Custom batching criteria from test metadata
    /// - Execution context and environment settings
    /// - Performance profile and threshold information
    /// 
    /// The method is designed to be thread-safe and can be called concurrently
    /// during test execution when multiple test cases complete simultaneously.
    /// </remarks>
    Task<TestCompletionQueueMessage> MapToQueueMessageAsync(
        TestCaseCompletedNotification notification,
        CancellationToken cancellationToken = default);
}
