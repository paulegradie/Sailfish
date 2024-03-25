using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.Presentation;

namespace Sailfish.Analysis.SailDiff;

internal interface ITrackingFileDirectoryReader
{
    List<string> FindTrackingFilesInDirectoryOrderedByLastModified(string directory, bool ascending = false);
}

internal class DefaultTrackingFileDirectoryReader : ITrackingFileDirectoryReader
{
    public List<string> FindTrackingFilesInDirectoryOrderedByLastModified(string directory, bool ascending = false)
    {
        var files = Directory.GetFiles(directory).Where(x => x.EndsWith(DefaultFileSettings.TrackingSuffix));
        if (ascending)
            return
            [
                .. files.OrderBy(file =>
                {
                    var fileInfo = new FileInfo(file);
                    return fileInfo.LastWriteTimeUtc;
                })
            ];

        return
        [
            .. files
                        .OrderByDescending(file => new FileInfo(file).LastWriteTimeUtc)
        ];
    }
}