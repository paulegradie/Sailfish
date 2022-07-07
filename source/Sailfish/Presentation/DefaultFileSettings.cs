using System;
using System.IO;
using System.Linq;
using System.Text;
using Accord.Collections;

namespace Sailfish.Presentation;

public static class DefaultFileSettings
{
    public static readonly string SortableFormat = "yyyy-dd-M--HH-mm-ss";
    public static readonly string TrackingSuffix = ".cvs.tracking";
    public static readonly string TagsPrefix = "tags-";
    private static readonly string UnderScore = "_";
    public static readonly Func<DateTime, string> DefaultPerformanceFileNameStem = (DateTime timestamp) => $"PerformanceResults_{timestamp.ToString(SortableFormat)}"; // sortable file name with date
    public static readonly Func<DateTime, string> DefaultTTestMarkdownFileName = (DateTime timeStamp) => $"t-test_{timeStamp.ToString(SortableFormat)}.md";
    public static readonly Func<DateTime, string> DefaultTTestCsvFileName = (DateTime timeStamp) => $"t-test_{timeStamp.ToString(SortableFormat)}.csv";
    public static readonly Func<DateTime, string> DefaultTrackingFileName = (timeStamp) => $"PerformanceTracking_{timeStamp.ToLocalTime().ToString(SortableFormat)}{TrackingSuffix}";

    public static string JoinTags(OrderedDictionary<string, string> tags)
    {
        if (tags.Count() == 0) return string.Empty;

        var result = new StringBuilder();
        result.Append(TagsPrefix);
        foreach (var tagPair in tags)
        {
            var joinedTag = string.Join(UnderScore, tagPair.Key, tagPair.Value, string.Empty);
            result.Append(joinedTag);
        }

        return result.ToString().TrimEnd(UnderScore.ToCharArray());
    }

    public static string AppendTagsToFilename(string fileName, OrderedDictionary<string, string> tags)
    {
        if (tags.Count() == 0) return fileName;
        var joinedTags = JoinTags(tags);

        if (fileName.EndsWith(TrackingSuffix))
        {
            var strippedFileName = fileName.Replace(TrackingSuffix, string.Empty);
            return $"{strippedFileName}.{joinedTags}" + TrackingSuffix;
        }
        else if (Path.HasExtension(fileName))
        {
            var filenameStem = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            return string.Join(".", filenameStem, joinedTags) + extension;
        }
        else
        {
            return string.Join(UnderScore, joinedTags);
        }
    }
}