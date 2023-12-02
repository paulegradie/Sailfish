using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Presentation;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public class CustomWriteToCloudHandler : INotificationHandler<TestRunCompletedNotification>
{
    private readonly ICloudWriter cloudWriter;
    private readonly IRunSettings runSettings;

    public CustomWriteToCloudHandler(ICloudWriter cloudWriter, IRunSettings runSettings)
    {
        this.cloudWriter = cloudWriter;
        this.runSettings = runSettings;
    }

    public async Task Handle(TestRunCompletedNotification notification, CancellationToken cancellationToken)
    {
        await cloudWriter.WriteToMyCloudStorageContainer(DefaultFileSettings.DefaultTrackingFileName(runSettings.TimeStamp), notification.ClassExecutionSummaries.ToList());
    }
}