using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Diagnostics.Environment;

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
    private readonly IEnvironmentHealthReportProvider? healthProvider;


    public FrameworkTestCaseEndNotificationHandler(ITestFrameworkWriter testFrameworkWriter)
    {
        this.testFrameworkWriter = testFrameworkWriter;
    }
    public FrameworkTestCaseEndNotificationHandler(ITestFrameworkWriter testFrameworkWriter, IEnvironmentHealthReportProvider healthProvider)
    {
        this.testFrameworkWriter = testFrameworkWriter;
        this.healthProvider = healthProvider;
    }


    public async Task Handle(FrameworkTestCaseEndNotification notification, CancellationToken cancellationToken)
    {
        await Task.Yield();

        var outcome = outcomeMap[notification.StatusCode];



        // Append environment health summary (if available) to the end of the per-test output
        var outputMessage = notification.TestOutputWindowMessage;
        var report = healthProvider?.Current;
        if (report is not null)
        {
            outputMessage = AppendEnvironmentHealthSummary(outputMessage, report);
        }

        var testResult = ConfigureTestResult(
            notification.TestCase,
            outcome,
            notification.Exception,
            notification.StartTime,
            notification.EndTime,
            notification.Duration,
            outputMessage);

        testFrameworkWriter.RecordEnd(notification.TestCase, outcome);
        testFrameworkWriter.RecordResult(testResult);
    }

    private static string AppendEnvironmentHealthSummary(string testOutputWindowMessage, EnvironmentHealthReport report)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(testOutputWindowMessage)) sb.AppendLine(testOutputWindowMessage.TrimEnd());
        sb.AppendLine();
        sb.AppendLine($"Sailfish Environment Health: {report.Score}/100 ({report.SummaryLabel})");
        foreach (var e in report.Entries.Take(6))
        {
            var rec = string.IsNullOrWhiteSpace(e.Recommendation) ? string.Empty : $" - {e.Recommendation}";
            sb.AppendLine($" - {e.Name}: {e.Status} ({e.Details}){rec}");
        }
        return sb.ToString();
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