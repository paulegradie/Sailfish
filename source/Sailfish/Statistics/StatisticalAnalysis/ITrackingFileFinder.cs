namespace Sailfish.Statistics.StatisticalAnalysis;

internal interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory);
}