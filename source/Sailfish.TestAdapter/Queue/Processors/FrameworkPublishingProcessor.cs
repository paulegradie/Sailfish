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
    #region Constants

    /// <summary>
    /// Metadata key for storing the TestCase object in test completion messages.
    /// </summary>
    private const string TestCaseMetadataKey = "TestCase";

    /// <summary>
    /// Metadata key for storing the formatted test output message.
    /// </summary>
    private const string TestOutputMessageMetadataKey = "FormattedMessage";

    /// <summary>
    /// Metadata key for storing the test execution start time.
    /// </summary>
    private const string StartTimeMetadataKey = "StartTime";

    /// <summary>
    /// Metadata key for storing the original exception object for failed tests.
    /// </summary>
    private const string ExceptionMetadataKey = "Exception";

    #endregion

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
            if (IsComparisonMethod(message))
            {
                Logger.Log(LogLevel.Debug,
                    "Skipping framework publishing for comparison method '{0}' - will be handled by MethodComparisonBatchProcessor",
                    message.TestCaseId);
                return;
            }

            // Extract required data from the message and metadata
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

    #region Data Extraction Methods

    /// <summary>
    /// Extracts the TestCase object from the message metadata.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The TestCase object, or a fallback TestCase if not found.</returns>
    private TestCase ExtractTestCase(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue(TestCaseMetadataKey, out var testCaseObj) && testCaseObj is TestCase testCase)
        {
            return testCase;
        }

        Logger.Log(LogLevel.Warning,
            "TestCase not found in metadata for test case '{0}'. Creating fallback TestCase.",
            message.TestCaseId);

        // Create a fallback TestCase to ensure framework publishing continues
        return new TestCase(message.TestCaseId, new Uri("executor://sailfish"), "Sailfish");
    }

    /// <summary>
    /// Extracts the formatted test output message from the message metadata.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The test output message, or a default message if not found.</returns>
    private string ExtractTestOutputMessage(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue(TestOutputMessageMetadataKey, out var messageObj) && messageObj is string outputMessage)
        {
            return outputMessage;
        }

        Logger.Log(LogLevel.Warning,
            "Test output message not found in metadata for test case '{0}'. Using default message.",
            message.TestCaseId);

        // Provide a basic fallback message
        return $"Test case '{message.TestCaseId}' completed with status: {(message.TestResult.IsSuccess ? "Success" : "Failed")}";
    }

    /// <summary>
    /// Extracts the test execution start time from the message metadata.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The start time, or a calculated fallback time if not found.</returns>
    private DateTimeOffset ExtractStartTime(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue(StartTimeMetadataKey, out var startTimeObj) && startTimeObj is DateTimeOffset startTime)
        {
            return startTime;
        }

        Logger.Log(LogLevel.Warning,
            "Start time not found in metadata for test case '{0}'. Calculating fallback start time.",
            message.TestCaseId);

        // Calculate fallback start time based on completion time and median duration
        var medianDuration = message.PerformanceMetrics.MedianMs;
        return message.CompletedAt.AddMilliseconds(-medianDuration);
    }

    /// <summary>
    /// Extracts the test execution end time from the message.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The end time from the CompletedAt field.</returns>
    private DateTimeOffset ExtractEndTime(TestCompletionQueueMessage message)
    {
        return message.CompletedAt;
    }

    /// <summary>
    /// Calculates the test execution duration.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <param name="startTime">The test start time.</param>
    /// <param name="endTime">The test end time.</param>
    /// <returns>The duration in milliseconds.</returns>
    private double CalculateDuration(TestCompletionQueueMessage message, DateTimeOffset startTime, DateTimeOffset endTime)
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
    /// Determines the status code based on the test result.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The appropriate StatusCode value.</returns>
    private StatusCode DetermineStatusCode(TestCompletionQueueMessage message)
    {
        return message.TestResult.IsSuccess ? StatusCode.Success : StatusCode.Failure;
    }

    /// <summary>
    /// Extracts the original exception from the message metadata or creates one from test result data.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The exception object, or null if the test was successful.</returns>
    private Exception? ExtractException(TestCompletionQueueMessage message)
    {
        // Return null for successful tests
        if (message.TestResult.IsSuccess)
        {
            return null;
        }

        // Try to get the original exception from metadata
        if (message.Metadata.TryGetValue(ExceptionMetadataKey, out var exceptionObj) && exceptionObj is Exception originalException)
        {
            return originalException;
        }

        // Create an exception from the test result data if original is not available
        if (!string.IsNullOrEmpty(message.TestResult.ExceptionMessage))
        {
            return new Exception(message.TestResult.ExceptionMessage);
        }

        // Fallback for failed tests without exception details
        return new Exception("Test failed without specific exception details");
    }

    /// <summary>
    /// Determines if a test case is a comparison method that should be handled by MethodComparisonBatchProcessor.
    /// </summary>
    /// <param name="message">The test completion queue message to check.</param>
    /// <returns>True if this is a comparison method, false otherwise.</returns>
    private static bool IsComparisonMethod(TestCompletionQueueMessage message)
    {
        // Check if the message has comparison metadata
        var hasComparisonGroup = message.Metadata.TryGetValue("ComparisonGroup", out var groupObj) &&
                                 groupObj is string group && !string.IsNullOrEmpty(group);

        return hasComparisonGroup;
    }

    #endregion
}
