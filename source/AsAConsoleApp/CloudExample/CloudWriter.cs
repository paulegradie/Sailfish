using System;
using System.Threading.Tasks;

namespace AsAConsoleApp.CloudExample;

class CloudWriter : ICloudWriter
{
    public Task WriteToMyCloudStorageContainer(string fileName, string content)
    {
        Console.WriteLine("Lets make believe this is writing to a cloud storage container (s3 or blob storage)");
        Console.WriteLine($"Like its writing to {fileName}");
        Console.WriteLine($"And it writing\r\n\r\n{content}");
        return Task.CompletedTask;
    }
}