using System.Linq;

namespace Sailfish.Statistics.StatisticalAnalysis;

internal class TrackingFileFinder : ITrackingFileFinder
{
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;

    public TrackingFileFinder(ITrackingFileDirectoryReader trackingFileDirectoryReader)
    {
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
    }

    public BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory)
    {
        var files = trackingFileDirectoryReader.DefaultReadDirectory(directory);
        return files.Count() < 2
            ? new BeforeAndAfterTrackingFiles(string.Empty, string.Empty)
            : new BeforeAndAfterTrackingFiles(files[0], files[1]);
    }
}