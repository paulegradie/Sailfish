using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Private.ExecutionCallbackNotifications;
using Sailfish.Logging;
using Sailfish.TestAdapter.Execution;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.FrameworkHandlers;

internal class ExceptionNotificationHandler(IAdapterConsoleWriter consoleWriter, ILogger logger)
    : INotificationHandler<ExceptionNotification>
{
    public async Task Handle(ExceptionNotification notification, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        logger.Log(LogLevel.Error, notification.Exception ?? new Exception("Undefined Exception"),
            "Encountered exception during test case execution");
        if (notification.TestInstanceContainer is null)
        {
            foreach (var testCase in notification.TestCaseGroup)
            {
                consoleWriter.RecordEnd(testCase, TestOutcome.Failed);
                consoleWriter.RecordResult(new TestResult(testCase));
            }
        }
        else
        {
            var currentTestCase =
                notification.TestInstanceContainer.GetTestCaseFromTestCaseGroupMatchingCurrentContainer(
                    notification.TestCaseGroup.Cast<TestCase>());
            consoleWriter.RecordEnd(currentTestCase, TestOutcome.Failed);
            consoleWriter.RecordResult(new TestResult(currentTestCase));
        }
    }
}