namespace Sailfish.Statistics.StatisticalAnalysis;

public interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory);
}