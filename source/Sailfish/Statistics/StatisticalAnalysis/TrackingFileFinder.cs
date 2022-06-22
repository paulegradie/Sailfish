using System.IO;
using System.Linq;

namespace Sailfish.Statistics.StatisticalAnalysis;

public class TrackingFileFinder : ITrackingFileFinder
{
    public BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory)
    {
        var files = Directory.GetFiles(Path.Combine(directory, "tracking_output"))
            .Where(x => x.EndsWith(".cvs.tracking"))
            .OrderByDescending(x => x)
            .ToList();

        return files.Count < 2
            ? new BeforeAndAfterTrackingFiles(files.First(), "")
            : new BeforeAndAfterTrackingFiles(files.First(), files[1]);
    }
}