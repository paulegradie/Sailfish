using System.IO;

namespace Sailfish.Statistics.StatisticalAnalysis;

public class BeforeAndAfterTrackingFiles
{
    public BeforeAndAfterTrackingFiles(string beforeFilePath, string afterFilePath)
    {
        BeforeFilePath = beforeFilePath;
        AfterFilePath = afterFilePath;
    }

    public string BeforeFilePath { get; set; }
    public string AfterFilePath { get; set; }
}