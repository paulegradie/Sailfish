using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VeerPerforma.Utils;

public interface IFileIo
{
    Task WriteToFile(string content, string filePath, CancellationToken cancellationToken);
}

public class FileIo : IFileIo
{
    public async Task WriteToFile(string content, string filePath, CancellationToken cancellationToken)
    {
        if (Directory.Exists(filePath)) throw new IOException("Cannot write to a directory");
        
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
    }
}