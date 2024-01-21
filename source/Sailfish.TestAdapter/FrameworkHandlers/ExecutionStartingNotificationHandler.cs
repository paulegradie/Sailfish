using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Private.ExecutionCallbackHandlers;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.TestProperties;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.FrameworkHandlers;

internal class ExecutionStartingNotificationHandler(IAdapterConsoleWriter consoleWriter) : INotificationHandler<ExecutionStartingNotification>
{
    private readonly IAdapterConsoleWriter consoleWriter = consoleWriter;

    public async Task Handle(ExecutionStartingNotification notification, CancellationToken cancellationToken)
    {
        var testCase = notification
            .TestCaseGroup
            .Cast<TestCase>()
            .Single(
                testCase =>
                    notification
                        .TestInstanceContainer
                        .TestCaseId
                        .DisplayName
                        .EndsWith(testCase.GetPropertyHelper(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty)));

        consoleWriter.RecordStart(testCase);
        await Task.Yield();
    }
}

