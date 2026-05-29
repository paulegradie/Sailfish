using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Logging;

namespace Sailfish.TestAdapter.Queue.Contracts;

/// <summary>
/// Single source of truth for keys and extraction helpers used to read auxiliary
/// data from <see cref="TestCompletionQueueMessage.Metadata"/>. Both producers
/// (TestCompletionMessageMapper) and consumers (FrameworkPublishingProcessor,
/// BatchTimeoutHandler) must go through here to stay in sync.
/// </summary>
internal static class MessageMetadata
{
    public static class Keys
    {
        public const string TestCase = "TestCase";
        public const string FormattedMessage = "FormattedMessage";
        public const string StartTime = "StartTime";
        public const string EndTime = "EndTime";
        public const string Exception = "Exception";
        public const string ComparisonGroup = "ComparisonGroup";
    }

    /// <summary>
    /// Reads the original <see cref="TestCase"/> from message metadata, falling back
    /// to a synthetic test case so framework publishing can always proceed.
    /// </summary>
    public static TestCase ExtractTestCase(TestCompletionQueueMessage message, ILogger logger)
    {
        if (message.Metadata.TryGetValue(Keys.TestCase, out var testCaseObj) && testCaseObj is TestCase testCase)
        {
            return testCase;
        }

        logger.Log(LogLevel.Warning,
            "TestCase not found in metadata for test case '{0}'. Creating fallback TestCase.",
            message.TestCaseId);

        return new TestCase(message.TestCaseId, new Uri("executor://sailfish"), "Sailfish");
    }

    /// <summary>
    /// Reads the formatted output message, falling back to a generated description.
    /// </summary>
    public static string ExtractFormattedMessage(TestCompletionQueueMessage message, ILogger logger)
    {
        if (message.Metadata.TryGetValue(Keys.FormattedMessage, out var messageObj) && messageObj is string outputMessage)
        {
            return outputMessage;
        }

        logger.Log(LogLevel.Warning,
            "Test output message not found in metadata for test case '{0}'. Using default message.",
            message.TestCaseId);

        return $"Test case '{message.TestCaseId}' completed with status: {(message.TestResult.IsSuccess ? "Success" : "Failed")}";
    }

    /// <summary>
    /// Reads the test start time, falling back to a value derived from the message's
    /// completion time and median duration.
    /// </summary>
    public static DateTimeOffset ExtractStartTime(TestCompletionQueueMessage message, ILogger logger)
    {
        if (message.Metadata.TryGetValue(Keys.StartTime, out var startTimeObj) && startTimeObj is DateTimeOffset startTime)
        {
            return startTime;
        }

        logger.Log(LogLevel.Warning,
            "Start time not found in metadata for test case '{0}'. Calculating fallback start time.",
            message.TestCaseId);

        return message.CompletedAt.AddMilliseconds(-message.PerformanceMetrics.MedianMs);
    }

    /// <summary>
    /// Reads the test end time, falling back to <see cref="TestCompletionQueueMessage.CompletedAt"/>.
    /// </summary>
    public static DateTimeOffset ExtractEndTime(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue(Keys.EndTime, out var endTimeObj) && endTimeObj is DateTimeOffset endTime)
        {
            return endTime;
        }

        return message.CompletedAt;
    }

    /// <summary>
    /// Reads the original exception from metadata for failed tests; falls back to one
    /// reconstructed from <see cref="TestCompletionQueueMessage.TestResult"/>.
    /// Returns null for successful tests.
    /// </summary>
    public static Exception? ExtractException(TestCompletionQueueMessage message)
    {
        if (message.TestResult.IsSuccess)
        {
            return null;
        }

        if (message.Metadata.TryGetValue(Keys.Exception, out var exceptionObj) && exceptionObj is Exception originalException)
        {
            return originalException;
        }

        if (!string.IsNullOrEmpty(message.TestResult.ExceptionMessage))
        {
            return new Exception(message.TestResult.ExceptionMessage);
        }

        return new Exception("Test failed without specific exception details");
    }

    /// <summary>
    /// Returns the test execution duration in milliseconds, preferring the recorded
    /// median timing and falling back to (end - start).
    /// </summary>
    public static double CalculateDuration(TestCompletionQueueMessage message, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (message.PerformanceMetrics.MedianMs > 0)
        {
            return message.PerformanceMetrics.MedianMs;
        }

        var duration = (endTime - startTime).TotalMilliseconds;
        return Math.Max(0, duration);
    }

    /// <summary>
    /// True when the message belongs to a comparison group and the test succeeded
    /// (failed comparison members are always published immediately).
    /// </summary>
    public static bool IsComparisonMember(TestCompletionQueueMessage message)
    {
        if (!message.TestResult.IsSuccess)
        {
            return false;
        }

        return message.Metadata.TryGetValue(Keys.ComparisonGroup, out var groupObj)
               && groupObj is string group
               && !string.IsNullOrEmpty(group);
    }
}
