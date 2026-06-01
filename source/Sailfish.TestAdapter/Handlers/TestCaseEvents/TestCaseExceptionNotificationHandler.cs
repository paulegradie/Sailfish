using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.TestAdapter.Display.VSTestFramework;

namespace Sailfish.TestAdapter.Handlers.TestCaseEvents;

internal class TestCaseExceptionNotificationHandler : INotificationHandler<TestCaseExceptionNotification>
{
    private readonly ILogger _logger;
    private readonly ITestFrameworkWriter _testFrameworkWriter;

    public TestCaseExceptionNotificationHandler(ITestFrameworkWriter testFrameworkWriter, ILogger logger)
    {
        _testFrameworkWriter = testFrameworkWriter;
        _logger = logger;
    }

    public async Task Handle(TestCaseExceptionNotification notification, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        _logger.Log(LogLevel.Error, notification.Exception ?? new TestAdapterException("Undefined Exception"), "Encountered exception during test case execution");
        if (notification.TestInstanceContainer is null)
        {
            // Whole-class activation/enumeration failure: the test instance was never built
            // (e.g. the constructor threw, an ISailfishFixture<T> failed to activate, or a
            // constructor dependency couldn't be resolved). Fail every test case in the group.
            foreach (var testCase in notification.TestCaseGroup.Cast<TestCase>())
            {
                _testFrameworkWriter.RecordEnd(testCase, TestOutcome.Failed);
                _testFrameworkWriter.RecordResult(CreateFailedResult(testCase, notification.Exception));
            }
        }
        else
        {
            var currentTestCase = notification
                .TestInstanceContainer
                .GetTestCaseFromTestCaseGroupMatchingCurrentContainer(notification.TestCaseGroup.Cast<TestCase>());
            _testFrameworkWriter.RecordEnd(currentTestCase, TestOutcome.Failed);
            _testFrameworkWriter.RecordResult(CreateFailedResult(currentTestCase, notification.Exception));
        }
    }

    // The TestResult.Outcome defaults to TestOutcome.None; if we record it as-is, VSTest/Rider
    // reports the cryptic "Outcome value None is not understood" (rendered as Inconclusive)
    // instead of a clean failure — the RecordEnd outcome above is not what Rider keys off.
    // We must set Outcome = Failed on the TestResult itself and attach the (flattened) exception
    // so the user sees the real reason. This mirrors FrameworkTestCaseEndNotificationHandler.ConfigureTestResult.
    private static TestResult CreateFailedResult(TestCase testCase, Exception? exception) => new(testCase)
    {
        Outcome = TestOutcome.Failed,
        DisplayName = testCase.DisplayName,
        ErrorMessage = FlattenMessage(exception),
        ErrorStackTrace = exception?.ToString() // ToString() already includes the inner-exception chain + stacks
    };

    // The actionable cause is usually an inner exception: TypeActivator wraps the original throw as
    // TestClassInstantiationException("Failed to resolve constructor dependencies..."), so flatten the
    // whole chain rather than surfacing only the outermost (uninformative) message.
    private static string FlattenMessage(Exception? exception)
    {
        if (exception is null) return "Test case failed before execution (no exception was captured).";
        var messages = new List<string>();
        for (var e = exception; e is not null; e = e.InnerException) messages.Add(e.Message);
        return string.Join("\n ---> ", messages);
    }
}