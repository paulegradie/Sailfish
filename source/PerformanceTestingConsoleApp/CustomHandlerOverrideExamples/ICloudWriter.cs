using System.Threading.Tasks;

namespace PerformanceTestingConsoleApp.CustomHandlerOverrideExamples;

public interface ICloudWriter
{
    Task WriteToMyCloudStorageContainer(string fileName, string content);
}