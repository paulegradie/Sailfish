using MediatR;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.TestSuite.Handlers;

public class TestRunCompletedNotificationHandler : INotificationHandler<TestRunCompletedNotification>
{
    private readonly IRunSettings runSettings;

    public TestRunCompletedNotificationHandler(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public async Task Handle(TestRunCompletedNotification notification, CancellationToken cancellationToken)
    {
        var outputDirectory = runSettings.LocalOutputDirectory;
        await File.WriteAllTextAsync(Path.Join(outputDirectory, "TestRunCompleted.txt"), "TestRunComplete", cancellationToken);
    }
}