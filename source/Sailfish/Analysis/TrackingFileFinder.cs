using System.Collections.Generic;

using System.Linq;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;

namespace Sailfish.Analysis;

internal class TrackingFileFinder : ITrackingFileFinder
{
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;

    public TrackingFileFinder(ITrackingFileDirectoryReader trackingFileDirectoryReader)
    {
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
    }

    public BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory, string beforeTarget, OrderedDictionary tags)
    {
        var files = trackingFileDirectoryReader.FindTrackingFilesInDirectoryOrderedByLastModified(directory);

        if (tags.Count > 0)
        {
            var joinedTags = DefaultFileSettings.JoinTags(tags);
            files = files.Where(x => x.Replace(DefaultFileSettings.TrackingSuffix, string.Empty).EndsWith(joinedTags)).ToList();
        }
        else
        {
            files = files.Where(x => !x.Contains(DefaultFileSettings.TagsPrefix)).ToList();
        }

        return files.Count < 2
            ? new BeforeAndAfterTrackingFiles(new List<string>(), new List<string>())
            : new BeforeAndAfterTrackingFiles(new List<string> { files[1] }, new List<string>() { files[0] });
    }
}