using System.Threading.Tasks;

namespace AsAConsoleApp.CloudExample;

public interface ICloudWriter
{
    Task WriteToMyCloudStorageContainer(string fileName, string content);
}