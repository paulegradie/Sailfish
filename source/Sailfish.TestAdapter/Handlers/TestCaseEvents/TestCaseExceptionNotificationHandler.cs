﻿using System.Linq;
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
    private readonly ILogger logger;
    private readonly ITestFrameworkWriter testFrameworkWriter;

    public TestCaseExceptionNotificationHandler(ITestFrameworkWriter testFrameworkWriter, ILogger logger)
    {
        this.testFrameworkWriter = testFrameworkWriter;
        this.logger = logger;
    }

    public async Task Handle(TestCaseExceptionNotification notification, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        logger.Log(LogLevel.Error, notification.Exception ?? new TestAdapterException("Undefined Exception"), "Encountered exception during test case execution");
        if (notification.TestInstanceContainer is null)
        {
            foreach (var testCase in notification.TestCaseGroup)
            {
                testFrameworkWriter.RecordEnd(testCase, TestOutcome.Failed);
                testFrameworkWriter.RecordResult(new TestResult(testCase));
            }
        }
        else
        {
            var currentTestCase = notification
                .TestInstanceContainer
                .GetTestCaseFromTestCaseGroupMatchingCurrentContainer(notification.TestCaseGroup.Cast<TestCase>());
            testFrameworkWriter.RecordEnd(currentTestCase, TestOutcome.Failed);
            testFrameworkWriter.RecordResult(new TestResult(currentTestCase));
        }
    }
}