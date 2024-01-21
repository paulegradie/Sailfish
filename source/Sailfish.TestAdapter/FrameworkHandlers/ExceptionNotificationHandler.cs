using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Private.ExecutionCallbackHandlers;
using Sailfish.TestAdapter.Execution;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.FrameworkHandlers;
internal class ExceptionNotificationHandler(IAdapterConsoleWriter consoleWriter) : INotificationHandler<ExceptionNotification>
{
    private readonly IAdapterConsoleWriter consoleWriter = consoleWriter;

    public async Task Handle(ExceptionNotification notification, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        if (notification.TestInstanceContainer is null)
        {
            foreach (var testCase in notification.TestCaseGroup) consoleWriter.RecordEnd(testCase, TestOutcome.Failed);
        }
        else
        {
            var currentTestCase = notification.TestInstanceContainer.GetTestCaseFromTestCaseGroupMatchingCurrentContainer(notification.TestCaseGroup.Cast<TestCase>());
            consoleWriter.RecordEnd(currentTestCase, TestOutcome.Failed);
        }
    }
}
