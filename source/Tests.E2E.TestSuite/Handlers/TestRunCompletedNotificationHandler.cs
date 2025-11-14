using MediatR;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.TestSuite.Handlers;

public class TestRunCompletedNotificationHandler : INotificationHandler<TestRunCompletedNotification>
{
    private readonly IRunSettings _runSettings;

    public TestRunCompletedNotificationHandler(IRunSettings runSettings)
    {
        this._runSettings = runSettings;
    }

    public async Task Handle(TestRunCompletedNotification notification, CancellationToken cancellationToken)
    {
        var outputDirectory = _runSettings.LocalOutputDirectory;
        await File.WriteAllTextAsync(Path.Join(outputDirectory, "TestRunCompleted.txt"), "TestRunComplete", cancellationToken);
    }
}