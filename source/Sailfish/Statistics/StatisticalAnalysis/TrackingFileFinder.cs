using System.Linq;
using Accord.Collections;
using Sailfish.Presentation;

namespace Sailfish.Statistics.StatisticalAnalysis;

internal class TrackingFileFinder : ITrackingFileFinder
{
    private readonly ITrackingFileDirectoryReader trackingFileDirectoryReader;

    public TrackingFileFinder(ITrackingFileDirectoryReader trackingFileDirectoryReader)
    {
        this.trackingFileDirectoryReader = trackingFileDirectoryReader;
    }

    public BeforeAndAfterTrackingFiles GetBeforeAndAfterTrackingFiles(string directory, OrderedDictionary<string, string> tags)
    {
        var files = trackingFileDirectoryReader.DefaultReadDirectory(directory);

        if (tags.Count() > 0)
        {
            var joinedTags = DefaultFileSettings.JoinTags(tags); // empty string
            files = files.Where(x => x.Replace(DefaultFileSettings.TrackingSuffix, string.Empty).EndsWith(joinedTags))
                .ToList();
        }
        else
        {
            files = files.Where(x => !x.Contains(DefaultFileSettings.TagsPrefix)).ToList();
        }

        return files.Count() < 2
            ? new BeforeAndAfterTrackingFiles(string.Empty, string.Empty)
            : new BeforeAndAfterTrackingFiles(files[1], files[0]);
    }
}