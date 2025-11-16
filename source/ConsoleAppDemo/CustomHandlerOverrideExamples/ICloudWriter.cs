using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace PerformanceTestingUserInvokedConsoleApp.CustomHandlerOverrideExamples;

public interface ICloudWriter
{
    Task WriteToMyCloudStorageContainer(string fileName, List<ClassExecutionSummaryTrackingFormat> content);
}