using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Private.ExecutionCallbackHandlers;
using Sailfish.TestAdapter.Execution;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.FrameworkHandlers;
internal class ExecutionDisabledNotificationHandler(IAdapterConsoleWriter consoleWriter) : INotificationHandler<ExecutionDisabledNotification>
{
    private readonly IAdapterConsoleWriter consoleWriter = consoleWriter;

    public async Task Handle(ExecutionDisabledNotification notification, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        void CreateDisabledResult(TestCase testCase)
        {
            var testResult = new TestResult(testCase)
            {
                ErrorMessage = "Test Disabled",
                ErrorStackTrace = null,
                Outcome = TestOutcome.Skipped,
                DisplayName = testCase.DisplayName,
                ComputerName = null,
                Duration = TimeSpan.Zero,
                StartTime = default,
                EndTime = default
            };

            consoleWriter.RecordEnd(testCase, testResult.Outcome);
            consoleWriter.RecordResult(testResult);
        }

        if (notification.TestCaseGroup is null) return; // no idea why this would happen, but exceptions are not the way
        if (notification.TestInstanceContainer is null) // then we've disabled the class - return all the results for the group
        {
            foreach (var testCase in notification.TestCaseGroup) CreateDisabledResult(testCase);
        }
        else // we've only disabled this method, send a single result
        {
            var currentTestCase = notification.TestInstanceContainer.GetTestCaseFromTestCaseGroupMatchingCurrentContainer(notification.TestCaseGroup.Cast<TestCase>());
            CreateDisabledResult(currentTestCase);
        }
    }
}

