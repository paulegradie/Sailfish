using System.Collections.Generic;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public interface ICloudWriter
{
    Task WriteToMyCloudStorageContainer(string fileName, List<IExecutionSummary> content);
}