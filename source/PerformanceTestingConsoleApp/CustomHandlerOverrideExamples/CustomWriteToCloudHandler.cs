using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public class CustomWriteToCloudHandler : INotificationHandler<WriteCurrentTrackingFileNotification>
{
    private readonly ICloudWriter cloudWriter;

    public CustomWriteToCloudHandler(ICloudWriter cloudWriter)
    {
        this.cloudWriter = cloudWriter;
    }

    public async Task Handle(WriteCurrentTrackingFileNotification request, CancellationToken cancellationToken)
    {
        await cloudWriter.WriteToMyCloudStorageContainer(request.DefaultFileName, request.ClassExecutionSummaries.ToList());
    }
}