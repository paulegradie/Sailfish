using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Presentation;

namespace Sailfish.Contracts.Private;

internal class SailfishUpdateTrackingDataNotification : INotification
{
}

internal class SailfishUpdateTrackingDataNotificationHandler : INotificationHandler<SailfishUpdateTrackingDataNotification>
{
    private readonly IRunSettings runSettings;

    public SailfishUpdateTrackingDataNotificationHandler(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public Task Handle(SailfishUpdateTrackingDataNotification notification, CancellationToken cancellationToken)
    {
        var output = runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }
    }
}