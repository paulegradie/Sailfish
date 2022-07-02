using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sailfish.Statistics.StatisticalAnalysis;

public class DefaultTrackingFileDirectoryReader : ITrackingFileDirectoryReader
{
    public virtual List<string> DefaultReadDirectory(string directory)
    {
        var files = Directory.GetFiles(Path.Combine(directory, "tracking_output"))
            .Where(x => x.EndsWith(".cvs.tracking"))
            .OrderByDescending(x => x)
            .ToList();
        return files;
    }
}