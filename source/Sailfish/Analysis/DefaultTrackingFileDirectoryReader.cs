using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.Presentation;

namespace Sailfish.Analysis;

internal class DefaultTrackingFileDirectoryReader : ITrackingFileDirectoryReader
{
    public virtual List<string> FindTrackingFilesInDirectory(string directory)
    {
        return Directory.GetFiles(directory)
            .Where(x => x.EndsWith(DefaultFileSettings.TrackingSuffix))
            .OrderByDescending(file =>
            {
                var fileInfo = new FileInfo(file);
                return fileInfo.LastWriteTimeUtc;
            })
            .ToList();
    }
}