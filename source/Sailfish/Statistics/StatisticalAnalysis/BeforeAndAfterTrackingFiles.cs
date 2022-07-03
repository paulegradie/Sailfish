namespace Sailfish.Statistics.StatisticalAnalysis;

internal class BeforeAndAfterTrackingFiles
{
    public BeforeAndAfterTrackingFiles(string beforeFilePath, string afterFilePath)
    {
        BeforeFilePath = beforeFilePath;
        AfterFilePath = afterFilePath;
    }

    public string BeforeFilePath { get; set; }
    public string AfterFilePath { get; set; }
}