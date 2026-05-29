using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Processors;

/// <summary>
/// Queue processor responsible for publishing test results to the VS Test Platform.
/// This processor is a critical component of the intercepting queue architecture that
/// converts test completion messages from the queue into framework notifications that
/// are sent to the VS Test Platform for display in test explorers and result windows.
/// </summary>
/// <remarks>
/// The FrameworkPublishingProcessor serves as the bridge between the queue system and
/// the VS Test Platform, ensuring that test results appear normally in IDEs while
/// enabling the powerful batch processing and cross-test-case analysis capabilities
/// of the intercepting queue architecture.
/// 
/// Key responsibilities:
/// - Extract test case data and results from queue messages
/// - Convert queue messages to FrameworkTestCaseEndNotification objects
/// - Publish notifications to the VS Test Platform via MediatR
/// - Handle both individual and batch test results (future enhancement)
/// - Maintain original framework contract and timing expectations
/// - Provide fallback mechanisms for missing or invalid data
/// - Support enhanced test results with comparison data (future enhancement)
/// 
/// The processor extracts the following data from TestCompletionQueueMessage metadata:
/// - TestCase object for framework identification
/// - Formatted test output messages for result display
/// - Timing information for test execution duration
/// - Exception details for failed tests
/// - Performance metrics for enhanced reporting
/// 
/// Error Handling:
/// The processor is designed to be resilient and never stop queue processing due to
/// data issues. Missing metadata is handled gracefully with appropriate logging and
/// sensible defaults to ensure test results are always published to the framework.
/// 
/// Thread Safety:
/// This processor inherits thread safety from TestCompletionQueueProcessorBase and
/// is designed to handle concurrent test execution scenarios where multiple test
/// cases complete simultaneously.
/// 
/// Future Enhancements:
/// - Batch processing support for cross-test-case analysis
/// - Enhanced result generation with comparison data
/// - Configurable result formatting and enhancement options
/// - Integration with historical data and trend analysis
/// </remarks>
public class FrameworkPublishingProcessor : TestCompletionQueueProcessorBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkPublishingProcessor"/> class.
    /// </summary>
    /// <param name="mediator">
    /// The MediatR mediator service for publishing framework notifications.
    /// Used to send FrameworkTestCaseEndNotification messages to the VS Test Platform.
    /// </param>
    /// <param name="logger">
    /// The logger service for recording processor operations and diagnostic information.
    /// Inherited from the base class for consistent logging patterns.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mediator"/> is null.
    /// </exception>
    /// <remarks>
    /// The mediator is essential for framework integration as it provides the communication
    /// channel to the VS Test Platform. The logger enables diagnostic tracking and error
    /// reporting for queue processing operations.
    /// </remarks>
    public FrameworkPublishingProcessor(IMediator mediator, ILogger logger) : base(logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    #endregion

    #region Core Processing

    /// <inheritdoc />
    protected override async Task ProcessTestCompletionCore(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        // Ensure we respect cancellation requests
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Check if this is a comparison method - if so, skip publishing here
            // The MethodComparisonBatchProcessor will handle publishing enhanced results
            if (MessageMetadata.IsComparisonMember(message))
            {
                Logger.Log(LogLevel.Debug,
                    "Skipping framework publishing for comparison method '{0}' - will be handled by MethodComparisonBatchProcessor",
                    message.TestCaseId);
                return;
            }

            // Extract required data from the message and metadata
            var testCase = MessageMetadata.ExtractTestCase(message, Logger);
            var testOutputMessage = MessageMetadata.ExtractFormattedMessage(message, Logger);
            var startTime = MessageMetadata.ExtractStartTime(message, Logger);
            var endTime = MessageMetadata.ExtractEndTime(message);
            var duration = MessageMetadata.CalculateDuration(message, startTime, endTime);
            var statusCode = message.TestResult.IsSuccess ? StatusCode.Success : StatusCode.Failure;
            var exception = MessageMetadata.ExtractException(message);

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

            Logger.Log(LogLevel.Debug,
                "Successfully published framework notification for test case '{0}' with status '{1}'",
                message.TestCaseId, statusCode);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, ex,
                "Failed to process framework publishing for test case '{0}': {1}",
                message.TestCaseId, ex.Message);
            
            // Re-throw to maintain error contract, but this will be handled by base class
            throw;
        }
    }

    #endregion
}
