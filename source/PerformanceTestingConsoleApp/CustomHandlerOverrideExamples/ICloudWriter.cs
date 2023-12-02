using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public interface ICloudWriter
{
    Task WriteToMyCloudStorageContainer(string fileName, List<ClassExecutionSummaryTrackingFormat> content);
}