using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

internal class CloudWriter : ICloudWriter
{
    public Task WriteToMyCloudStorageContainer(string fileName, List<ClassExecutionSummaryTrackingFormat> content)
    {
        Console.WriteLine("Lets make believe this is writing to a cloud storage container (s3 or blob storage)");
        Console.WriteLine($"Like its writing to {fileName}");
        Console.WriteLine($"And it writing\r\n\r\n{content}");
        return Task.CompletedTask;
    }
}