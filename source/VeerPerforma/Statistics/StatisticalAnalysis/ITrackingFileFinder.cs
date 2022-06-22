namespace VeerPerforma.Statistics.StatisticalAnalysis;

public interface ITrackingFileFinder
{
    BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory);
}