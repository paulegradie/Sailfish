using System.IO;
using System.Threading.Tasks;
using Sailfish.Statistics.StatisticalAnalysis;

namespace Sailfish.Presentation;

public class BeforeAndAfterStreamReader : IBeforeAndAfterStreamReader
{
    public async Task<BeforeAndAfterTrackingFiles> ReadBeforeAndAfterStream(FileStream before, FileStream after)
    {
        var beforeData = await ReadContent(before);
        var afterData = await ReadContent(after);

        return new BeforeAndAfterTrackingFiles(beforeData, afterData);
    }

    private async Task<string> ReadContent(FileStream fileStream)
    {
        var reader = new StreamReader(fileStream);
        var beforeContent = await reader.ReadToEndAsync();

        reader.Close();
        fileStream.Close();

        return beforeContent;
    }
}