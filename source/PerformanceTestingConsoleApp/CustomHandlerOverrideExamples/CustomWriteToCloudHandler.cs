using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public class CustomWriteToCloudHandler : INotificationHandler<WriteCurrentTrackingFileCommand>
{
    private readonly ICloudWriter cloudWriter;

    public CustomWriteToCloudHandler(ICloudWriter cloudWriter)
    {
        this.cloudWriter = cloudWriter;
    }

    public async Task Handle(WriteCurrentTrackingFileCommand request, CancellationToken cancellationToken)
    {
        await cloudWriter.WriteToMyCloudStorageContainer(request.DefaultFileName, request.TrackingDataFormats.Csv);
        Console.WriteLine("-------------- NOW AS JSON ---------------");
        await cloudWriter.WriteToMyCloudStorageContainer(request.DefaultFileName, request.TrackingDataFormats.Json);
    }
}