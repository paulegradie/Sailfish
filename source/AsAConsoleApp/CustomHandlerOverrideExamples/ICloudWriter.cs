using System.Threading.Tasks;

namespace AsAConsoleApp.CustomHandlerOverrideExamples;

public interface ICloudWriter
{
    Task WriteToMyCloudStorageContainer(string fileName, string content);
}