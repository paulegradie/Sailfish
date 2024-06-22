using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Execution;
using Sailfish.TestAdapter.Display.VSTestFramework;

namespace Sailfish.TestAdapter.Handlers.FrameworkHandlers;

internal record FrameworkTestCaseEndNotification(
    string TestOutputWindowMessage,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    double Duration,
    TestCase TestCase,
    StatusCode StatusCode,
    Exception? Exception
) : INotification;

internal class FrameworkTestCaseEndNotificationHandler : INotificationHandler<FrameworkTestCaseEndNotification>
{
    private readonly Dictionary<StatusCode, TestOutcome> outcomeMap = new() { { StatusCode.Success, TestOutcome.Passed }, { StatusCode.Failure, TestOutcome.Failed } };
    private readonly ITestFrameworkWriter testFrameworkWriter;

    public FrameworkTestCaseEndNotificationHandler(ITestFrameworkWriter testFrameworkWriter)
    {
        this.testFrameworkWriter = testFrameworkWriter;
    }

    public async Task Handle(FrameworkTestCaseEndNotification notification, CancellationToken cancellationToken)
    {
        await Task.Yield();

        var outcome = outcomeMap[notification.StatusCode];

        var testResult = ConfigureTestResult(
            notification.TestCase,
            outcome,
            notification.Exception,
            notification.StartTime,
            notification.EndTime,
            notification.Duration,
            notification.TestOutputWindowMessage);

        testFrameworkWriter.RecordEnd(notification.TestCase, outcome);
        testFrameworkWriter.RecordResult(testResult);
    }

    private static TestResult ConfigureTestResult(
        TestCase currentTestCase,
        TestOutcome outcome,
        Exception? exception,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        double duration,
        string testOutputWindowMessage)
    {
        var testResult = new TestResult(currentTestCase)
        {
            Outcome = outcome,
            DisplayName = currentTestCase.DisplayName,
            StartTime = startTime,
            EndTime = endTime,
            Duration = TimeSpan.FromMilliseconds(double.IsNaN(duration) ? 0 : duration)
        };

        if (exception is null)
        {
            testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, testOutputWindowMessage));
            return testResult;
        }

        var exMessage = exception.Message;
        if (exception.StackTrace?.Contains("InvocationReflectionExtensionMethods.TryInvoke") ?? false)
            exMessage = $"An unhandled exception was thrown in your SailfishMethod:\n[{currentTestCase.FullyQualifiedName}] ";

        var stackTrace = "\nStackTrace:\n\n" + exception.StackTrace;
        if (exception.InnerException is not null) stackTrace = "\nInner StackTrace:\n\n" + exception.InnerException + "\n" + stackTrace;

        testResult.ErrorStackTrace = stackTrace;
        testResult.ErrorMessage = exMessage;
        return testResult;
    }
}