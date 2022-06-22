using System.IO;

namespace Sailfish.Statistics.StatisticalAnalysis;

public class BeforeAndAfterTrackingFiles
{
    public BeforeAndAfterTrackingFiles(string before, string after)
    {
        Before = before;
        After = after;
    }

    public string Before { get; set; }
    public string After { get; set; }

    public string GetFileStem(string path)
    {
        return Path.GetFileName(path);
    }
}