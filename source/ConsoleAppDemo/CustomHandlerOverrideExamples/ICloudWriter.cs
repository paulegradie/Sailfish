using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PerformanceTestingUserInvokedConsoleApp.CustomHandlerOverrideExamples;

public interface ICloudWriter
{
    Task WriteToMyCloudStorageContainer(string fileName, List<ClassExecutionSummaryTrackingFormat> content);
}