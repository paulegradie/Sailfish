using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Execution;
using Sailfish.TestAdapter.Display.VSTestFramework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly ITestFrameworkWriter testFrameworkWriter;

    public FrameworkTestCaseEndNotificationHandler(ITestFrameworkWriter testFrameworkWriter)
    {
        this.testFrameworkWriter = testFrameworkWriter;
    }

    private readonly Dictionary<StatusCode, TestOutcome> outcomeMap = new() { { StatusCode.Success, TestOutcome.Passed }, { StatusCode.Failure, TestOutcome.Failed } };

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

        testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, testOutputWindowMessage));

        if (exception is null)
        {
            return testResult;
        }

        testResult.ErrorMessage = exception.Message;
        testResult.ErrorStackTrace = exception.StackTrace;
        testResult.Messages.Add(
            new TestResultMessage(TestResultMessage.StandardErrorCategory,
                exception.Message));

        return testResult;
    }
}