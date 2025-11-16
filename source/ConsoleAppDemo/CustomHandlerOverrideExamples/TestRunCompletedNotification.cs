using MediatR;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Presentation;

namespace PerformanceTestingUserInvokedConsoleApp.CustomHandlerOverrideExamples;

public class TestRunCompletedNotificationHandler : INotificationHandler<TestRunCompletedNotification>
{
    private readonly ICloudWriter _cloudWriter;
    private readonly IRunSettings _runSettings;

    public TestRunCompletedNotificationHandler(ICloudWriter cloudWriter, IRunSettings runSettings)
    {
        _cloudWriter = cloudWriter;
        _runSettings = runSettings;
    }

    public async Task Handle(TestRunCompletedNotification notification, CancellationToken cancellationToken)
    {
        await _cloudWriter.WriteToMyCloudStorageContainer(DefaultFileSettings.DefaultTrackingFileName(_runSettings.TimeStamp), notification.ClassExecutionSummaries.ToList());
    }
}