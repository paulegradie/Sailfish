using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.Presentation;

namespace Sailfish.Analysis.SailDiff;

internal class DefaultTrackingFileDirectoryReader : ITrackingFileDirectoryReader
{
    public virtual List<string> FindTrackingFilesInDirectoryOrderedByLastModified(string directory, bool ascending = false)
    {
        var files = Directory.GetFiles(directory).Where(x => x.EndsWith(DefaultFileSettings.TrackingSuffix));
        if (ascending)
        {
            return files.OrderBy(file =>
                {
                    var fileInfo = new FileInfo(file);
                    return fileInfo.LastWriteTimeUtc;
                })
                .ToList();
        }

        return files
            .OrderByDescending(file => new FileInfo(file).LastWriteTimeUtc)
            .ToList();
    }
}