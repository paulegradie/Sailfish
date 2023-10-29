using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Contracts.Serialization.V1;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public interface ICloudWriter
{
    Task WriteToMyCloudStorageContainer(string fileName, List<ClassExecutionSummaryTrackingFormat> content);
}