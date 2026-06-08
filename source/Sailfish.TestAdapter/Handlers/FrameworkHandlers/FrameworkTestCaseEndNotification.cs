using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sailfish.Diagnostics.Environment;

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Display.VSTestFramework;

namespace Sailfish.TestAdapter.Handlers.FrameworkHandlers;

internal record FrameworkTestCaseEndNotification : INotification
{
    public FrameworkTestCaseEndNotification(string TestOutputWindowMessage,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        double Duration,
        TestCase TestCase,
        StatusCode StatusCode,
        Exception? Exception)
    {
        this.TestOutputWindowMessage = TestOutputWindowMessage;
        this.StartTime = StartTime;
        this.EndTime = EndTime;
        this.Duration = Duration;
        this.TestCase = TestCase;
        this.StatusCode = StatusCode;
        this.Exception = Exception;
    }

    public string TestOutputWindowMessage { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public double Duration { get; init; }
    public TestCase TestCase { get; init; }
    public StatusCode StatusCode { get; init; }
    public Exception? Exception { get; init; }

    public void Deconstruct(out string TestOutputWindowMessage, out DateTimeOffset StartTime, out DateTimeOffset EndTime, out double Duration, out TestCase TestCase, out StatusCode StatusCode, out Exception? Exception)
    {
        TestOutputWindowMessage = this.TestOutputWindowMessage;
        StartTime = this.StartTime;
        EndTime = this.EndTime;
        Duration = this.Duration;
        TestCase = this.TestCase;
        StatusCode = this.StatusCode;
        Exception = this.Exception;
    }
}

internal class FrameworkTestCaseEndNotificationHandler : INotificationHandler<FrameworkTestCaseEndNotification>
{
    private readonly Dictionary<StatusCode, TestOutcome> _outcomeMap = new() { { StatusCode.Success, TestOutcome.Passed }, { StatusCode.Failure, TestOutcome.Failed } };
    private static readonly Uri SailfishReportUri = new("sailfish://report/v1");
    private readonly ITestFrameworkWriter _testFrameworkWriter;
    private readonly IEnvironmentHealthReportProvider? _healthProvider;
    private readonly IRunSettings? _runSettings;


    public FrameworkTestCaseEndNotificationHandler(ITestFrameworkWriter testFrameworkWriter)
    {
        _testFrameworkWriter = testFrameworkWriter;
    }
    public FrameworkTestCaseEndNotificationHandler(ITestFrameworkWriter testFrameworkWriter, IEnvironmentHealthReportProvider healthProvider)
    {
        _testFrameworkWriter = testFrameworkWriter;
        _healthProvider = healthProvider;
    }

    // Preferred ctor: the run settings let us write each case's report to disk and attach it (DI picks the
    // longest satisfiable ctor). When run settings aren't available the handler still works, just without attachments.
    public FrameworkTestCaseEndNotificationHandler(ITestFrameworkWriter testFrameworkWriter, IEnvironmentHealthReportProvider healthProvider, IRunSettings runSettings)
        : this(testFrameworkWriter, healthProvider)
    {
        _runSettings = runSettings;
    }


    public async Task Handle(FrameworkTestCaseEndNotification notification, CancellationToken cancellationToken)
    {
        await Task.Yield();

        var outcome = _outcomeMap[notification.StatusCode];



        // Append environment health summary (if available) to the end of the per-test output
        var outputMessage = notification.TestOutputWindowMessage;
        var report = _healthProvider?.Current;
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

        TryAttachReport(testResult, notification.TestCase, outputMessage);

        _testFrameworkWriter.RecordEnd(notification.TestCase, outcome);
        _testFrameworkWriter.RecordResult(testResult);
    }

    /// <summary>
    ///     Writes the test case's formatted Sailfish report to a markdown file and attaches it to the result, so
    ///     it surfaces as a downloadable artifact in the IDE result pane and in .trx / CI runs — not just as inline
    ///     console output. Named with the per-run session id so it correlates with the other TestSession_* files.
    ///     Best-effort: a write/attach failure never fails the test.
    /// </summary>
    private void TryAttachReport(TestResult testResult, TestCase testCase, string report)
    {
        if (_runSettings is null || string.IsNullOrWhiteSpace(report)) return;

        try
        {
            var reportsDirectory = Path.Combine(_runSettings.LocalOutputDirectory, "test_reports");
            var sessionId = DefaultFileSettings.SessionId(_runSettings.TimeStamp);
            var name = SanitizeFileName(testCase.DisplayName ?? testCase.FullyQualifiedName);
            var path = Path.Combine(reportsDirectory, $"TestSession_{sessionId}_{name}.md");

            // Runtime adapter IO. RS1035 targets analyzer assemblies; this project only references Roslyn for
            // discovery parsing, so file IO is suppressed here exactly as elsewhere in the adapter.
#pragma warning disable RS1035
            Directory.CreateDirectory(reportsDirectory);
            File.WriteAllText(path, report);
#pragma warning restore RS1035

            var attachmentSet = new AttachmentSet(SailfishReportUri, "Sailfish report");
            attachmentSet.Attachments.Add(new UriDataAttachment(new Uri(path), $"{testCase.DisplayName} (Sailfish report)"));
            testResult.Attachments.Add(attachmentSet);
        }
        catch
        {
            // Best-effort: never fail a test because its report couldn't be written or attached.
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
        return name.Length > 120 ? name[..120] : name;
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
        // Attribute the failure to the user's code when the throw unwound through a Sailfish invocation
        // chokepoint: lifecycle methods run via reflection (TryInvoke); the timed method now runs via a
        // compiled delegate awaited inside CoreInvoker.
        var stackTrace0 = exception.StackTrace ?? string.Empty;
        if (stackTrace0.Contains("InvocationReflectionExtensionMethods.TryInvoke") || stackTrace0.Contains("Sailfish.Execution.CoreInvoker"))
            exMessage = $"An unhandled exception was thrown in your SailfishMethod:\n[{currentTestCase.FullyQualifiedName}] ";

        var stackTrace = "\nStackTrace:\n\n" + exception.StackTrace;
        if (exception.InnerException is not null) stackTrace = "\nInner StackTrace:\n\n" + exception.InnerException + "\n" + stackTrace;

        testResult.ErrorStackTrace = stackTrace;
        testResult.ErrorMessage = exMessage;
        return testResult;
    }
}