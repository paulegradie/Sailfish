using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.TestAdapter.Display.VSTestFramework;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Handlers.TestCaseEvents;

internal class TestCaseStartedNotificationHandler : INotificationHandler<TestCaseStartedNotification>
{
    private readonly ITestFrameworkWriter _testFrameworkWriter;

    public TestCaseStartedNotificationHandler(ITestFrameworkWriter testFrameworkWriter)
    {
        _testFrameworkWriter = testFrameworkWriter;
    }

    public async Task Handle(TestCaseStartedNotification notification, CancellationToken cancellationToken)
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
                        .EndsWith(testCase.GetPropertyHelper(SailfishManagedProperty
                            .SailfishDisplayNameDefinitionProperty)));

        _testFrameworkWriter.RecordStart(testCase);
        await Task.Yield();
    }
}