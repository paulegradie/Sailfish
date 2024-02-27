using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.TestAdapter.Display.VSTestFramework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Handlers.TestCaseEvents;

internal class TestCaseDisabledNotificationHandler : INotificationHandler<TestCaseDisabledNotification>
{
    private readonly ITestFrameworkWriter testFrameworkWriter;

    public TestCaseDisabledNotificationHandler(ITestFrameworkWriter testFrameworkWriter)
    {
        this.testFrameworkWriter = testFrameworkWriter;
    }

    public async Task Handle(TestCaseDisabledNotification notification, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        if (notification.TestCaseGroup is null) return; // no idea why this would happen, but exceptions are not the way
        if (notification.TestInstanceContainer is null || notification.DisableTheGroup) // then we've disabled the class - return all the results for the group
        {
            foreach (var testCase in notification.TestCaseGroup) CreateDisabledResult(testCase);
        }
        else // we've only disabled this method, send a single result
        {
            var currentTestCase = notification.TestInstanceContainer.GetTestCaseFromTestCaseGroupMatchingCurrentContainer(notification.TestCaseGroup.Cast<TestCase>());
            CreateDisabledResult(currentTestCase);
        }
    }

    private void CreateDisabledResult(TestCase testCase)
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

        testFrameworkWriter.RecordEnd(testCase, testResult.Outcome);
        testFrameworkWriter.RecordResult(testResult);
    }
}