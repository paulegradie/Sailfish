using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.Presentation;

namespace Sailfish.Analysis;

internal class DefaultTrackingFileDirectoryReader : ITrackingFileDirectoryReader
{
    public virtual List<string> DefaultReadDirectory(string directory)
    {
        var files = Directory.GetFiles(directory)
            .Where(x => x.EndsWith(DefaultFileSettings.TrackingSuffix))
            .OrderByDescending(x => x)
            .ToList();
        return files;
    }
}